using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerServer.Models.Helpers
{
#pragma warning disable CS8618
    public class Register
    {
        [Required(ErrorMessage = "Необходимо ввести логин")]
        [Display(Name = "Логин")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Необходимо ввести пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Необходимо указать фамилию")]
        [Display(Name = "Фамилия")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Необходимо указать имя")]
        [Display(Name = "Имя")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Необходимо указать отчество")]
        [Display(Name = "Отчество")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string Patronymic { get; set; }

        [Required(ErrorMessage = "Необходимо указать должность")]
        [Display(Name = "Должность")]
        public string Position { get; set; }
    }
    #pragma warning restore CS8618
}
