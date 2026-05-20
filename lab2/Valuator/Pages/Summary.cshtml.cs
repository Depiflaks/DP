using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Valuator.Services;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IStorageService _storage;

    public SummaryModel(ILogger<SummaryModel> logger, IStorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        Rank = _storage.GetRank(id);
        Similarity = _storage.GetSimilarity(id);
    }
}
