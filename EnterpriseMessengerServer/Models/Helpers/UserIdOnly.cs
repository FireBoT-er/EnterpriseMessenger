using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerServer.Models.Helpers
{
#pragma warning disable CS8618
    public class UserIdOnly
    {
        [Required(ErrorMessage = "Необходимо указать ID пользователя")]
        [Display(Name = "ID пользователя")]
        public string Id { get; set; }
    }
    #pragma warning restore CS8618
}
