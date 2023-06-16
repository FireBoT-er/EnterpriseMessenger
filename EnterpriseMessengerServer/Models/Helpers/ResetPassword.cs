using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerServer.Models.Helpers
{
#pragma warning disable CS8618
    public class ResetPassword
    {
        [Required(ErrorMessage = "Необходимо указать ID пользователя")]
        [Display(Name = "ID пользователя")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Необходимо ввести новый пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }
    }
    #pragma warning restore CS8618
}
