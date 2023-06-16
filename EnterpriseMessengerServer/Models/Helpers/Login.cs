using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerServer.Models.Helpers
{
#pragma warning disable CS8618
    public class Login
    {
        [Required(ErrorMessage = "Необходимо ввести логин")]
        [Display(Name = "Логин")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Необходимо ввести пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
    }
    #pragma warning restore CS8618
}
