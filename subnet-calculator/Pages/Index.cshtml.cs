using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using subnet_calculator.Helpers;

namespace subnet_calculator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public string InputIP { get; set; } = "";

    [BindProperty]
    public int Cidr { get; set; } = 24;

    [BindProperty]
    public int? TargetHost { get; set; }

    public SubnetResult? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InputIP))
            {
                ErrorMessage = "Please enter an IP address.";
                return;
            }

            Result = SubnetHelper.Calculate(InputIP, Cidr, TargetHost);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }
}
