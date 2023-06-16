using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerServer.Models.Helpers
{
#pragma warning disable CS8618
    public class ChangeFullNamePosition
    {
        [Required(ErrorMessage = "Необходимо указать ID пользователя")]
        [Display(Name = "ID пользователя")]
        public string Id { get; set; }

        [Display(Name = "Фамилия")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string? Surname { get; set; }

        [Display(Name = "Имя")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string? Name { get; set; }

        [Display(Name = "Отчество")]
        [RegularExpression(@"[а-яА-ЯёЁ-]+", ErrorMessage = "Только русский алфавит")]
        public string? Patronymic { get; set; }

        [Display(Name = "Должность")]
        public string? Position { get; set; }
    }
    #pragma warning restore CS8618
}
