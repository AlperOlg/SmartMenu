using System.ComponentModel.DataAnnotations;

namespace Project.Web.Models;

public class Verify2FAViewModel
{
    [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
    [Display(Name = "Doğrulama Kodu")]
    [DataType(DataType.Text)]
    [StringLength(8, MinimumLength = 4, ErrorMessage = "Geçerli bir doğrulama kodu giriniz.")]
    public string Code { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public bool RememberMe { get; set; }

    /// <summary>Maskelenmiş e-posta (örn. alp*****@gmail.com).</summary>
    public string? MaskedEmail { get; set; }
}
