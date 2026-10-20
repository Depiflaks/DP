import base64
import html
import http.cookiejar
import json
import os
import re
import secrets
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BASE_URL = os.environ.get("BASE_URL", "http://localhost:8080")
PROJECT = os.environ.get("COMPOSE_PROJECT_NAME", "lab7")


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        return None


class Browser:
    def __init__(self):
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect()
        )

    def request(self, path, method="GET", data=None, headers=None):
        request = urllib.request.Request(BASE_URL + path, data, headers or {}, method=method)
        try:
            response = self.opener.open(request, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.status, response.headers, response.read().decode()

    def submit(self, path, fields):
        status, _, body = self.request(path)
        assert status == 200, (path, status)
        token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', body)
        assert token, "Missing CSRF token"
        fields = dict(fields, __RequestVerificationToken=html.unescape(token.group(1)))
        data = urllib.parse.urlencode(fields).encode()
        return self.request(path, "POST", data, {"Content-Type": "application/x-www-form-urlencoded"})


def credentials(login, password):
    return {"Input.Login": login, "Input.Password": password}


def wait_for_app():
    browser = Browser()
    for _ in range(30):
        try:
            if browser.request("/Login")[0] == 200:
                return
        except (OSError, urllib.error.URLError):
            pass
        time.sleep(1)
    raise AssertionError("Application did not start")


def check_accounts():
    login = "check_" + secrets.token_hex(6)
    password = secrets.token_urlsafe(24)
    author, other = Browser(), Browser()
    status, headers, _ = author.submit("/Register", credentials(login, password))
    assert status == 302
    cookies = "\n".join(headers.get_all("Set-Cookie", [])).lower()
    assert "httponly" in cookies
    assert "samesite=strict" in cookies
    assert other.submit("/Register", credentials(login.upper(), password))[0] == 200
    assert other.request("/")[0] == 302, "Duplicate login signed in"
    assert other.submit("/Login", credentials(login, secrets.token_urlsafe(24)))[0] == 200
    assert other.request("/")[0] == 302, "Wrong password signed in"
    assert other.submit("/Login", credentials(login.upper(), password))[0] == 302
    assert other.request("/")[0] == 200
    assert author.request("/Logout")[0] == 200
    assert author.request("/")[0] == 200, "GET logged the user out"
    assert author.submit("/Logout", {})[0] == 302
    assert author.request("/")[0] == 302
    assert author.submit("/Login", credentials(login, password))[0] == 302
    other = Browser()
    assert other.submit("/Register", credentials(login + "_other", password))[0] == 302
    print("PASS: registration, unique login, password verification, login and logout")
    return author, other


def check_summary(author, other):
    text = "Security check " + secrets.token_hex(12)
    status, headers, _ = author.submit("/", {"Country": "France", "text": text})
    assert status == 302
    location = headers["Location"]
    text_id = urllib.parse.parse_qs(urllib.parse.urlparse(location).query)["id"][0]
    path = "/Summary?id=" + text_id
    assert Browser().request(path)[0] == 302
    assert other.request(path)[0] == 404, "Another user can read the summary"
    assert author.request("/Summary?id=invalid")[0] == 404
    for _ in range(30):
        status, headers, body = author.request(path)
        assert status == 200
        assert "no-store" in headers.get("Cache-Control", "")
        if 'data-completed="true"' in body:
            print("PASS: async calculation and summary access restricted to author")
            return text_id
        time.sleep(1)
    raise AssertionError("Rank calculation did not complete")


def hub_request(browser, path, message):
    data = (json.dumps(message) + "\x1e").encode()
    status, _, _ = browser.request(path, "POST", data, {"Content-Type": "text/plain"})
    assert status == 200


def check_subscription(browser, text_id, allowed):
    status, _, body = browser.request("/hubs/summary/negotiate?negotiateVersion=1", "POST", b"")
    assert status == 200
    token = urllib.parse.quote(json.loads(body)["connectionToken"])
    path = "/hubs/summary?id=" + token
    assert browser.request(path)[0] == 200
    hub_request(browser, path, {"protocol": "json", "version": 1})
    assert browser.request(path)[0] == 200
    hub_request(browser, path, {
        "type": 1, "invocationId": "1", "target": "Subscribe", "arguments": [text_id]
    })
    messages = []
    while not any(item.get("type") == 3 for item in messages):
        status, _, body = browser.request(path)
        assert status == 200
        messages.extend(json.loads(item) for item in body.split("\x1e") if item)
    completion = next(item for item in messages if item.get("type") == 3)
    notifications = [item for item in messages if item.get("target") == "RankCalculated"]
    assert ("error" not in completion) == allowed, completion
    assert bool(notifications) == allowed, messages
    assert browser.request(path, "DELETE")[0] == 202


def check_hub(author, other, text_id):
    assert Browser().request("/hubs/summary/negotiate?negotiateVersion=1", "POST", b"")[0] == 401
    check_subscription(other, text_id, False)
    check_subscription(author, text_id, True)
    print("PASS: SignalR rejects anonymous clients and subscriptions to another user's text")


def compose(*arguments):
    result = subprocess.run(
        ["docker", "compose", "-p", PROJECT, *arguments], cwd=ROOT,
        check=True, capture_output=True, text=True
    )
    return result.stdout + result.stderr


def check_redis():
    for region in ("main", "ru", "eu", "asia"):
        service = "redis-" + region
        assert "NOAUTH" in compose("exec", "-T", service, "redis-cli", "PING")
        output = compose("exec", "-T", "-e", "REDISCLI_AUTH=invalid", service, "redis-cli", "PING")
        assert "WRONGPASS" in output
        output = compose("exec", "-T", service, "sh", "-c", 'REDISCLI_AUTH="$REDIS_PASSWORD" redis-cli PING')
        assert output.strip() == "PONG"
    print("PASS: all four Redis instances require the configured password")


def rabbit_status(user, password):
    url = os.environ.get("RABBITMQ_URL", "http://localhost:15672") + "/api/whoami"
    auth = base64.b64encode((user + ":" + password).encode()).decode()
    request = urllib.request.Request(url, headers={"Authorization": "Basic " + auth})
    try:
        with urllib.request.urlopen(request, timeout=10) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code


def check_rabbitmq():
    values = dict(line.split("=", 1) for line in (ROOT / ".env").read_text().splitlines() if "=" in line)
    user = os.environ.get("RABBITMQ_USER", values["RABBITMQ_USER"])
    password = os.environ.get("RABBITMQ_PASSWORD", values["RABBITMQ_PASSWORD"])
    assert rabbit_status(user, password) == 200
    assert rabbit_status(user, secrets.token_urlsafe(24)) == 401
    print("PASS: RabbitMQ accepts the configured password and rejects a wrong password")


def main():
    wait_for_app()
    check_redis()
    check_rabbitmq()
    author, other = check_accounts()
    text_id = check_summary(author, other)
    check_hub(author, other, text_id)


if __name__ == "__main__":
    main()
