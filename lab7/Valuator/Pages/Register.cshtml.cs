using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Authentication;

namespace Valuator.Pages;

[AllowAnonymous]
public class RegisterModel(UserStore users) : PageModel
{
    [BindProperty]
    public Credentials Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await users.RegisterAsync(Input.Login, Input.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Этот логин уже занят.");
            return Page();
        }

        await HttpContext.SignInAsync(user.ToPrincipal());
        return RedirectToPage("/Index");
    }
}
