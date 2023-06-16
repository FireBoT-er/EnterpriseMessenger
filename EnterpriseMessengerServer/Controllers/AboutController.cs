using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseMessengerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AboutController : Controller
    {
        [HttpGet]
        public IActionResult About()
        {
            return StatusCode(StatusCodes.Status418ImATeapot, new
            {
                Message = "Об авторе: " +
                          "Емельянов Владислав; " +
                          "vlademel2016@yandex.ru; " +
                          "МИВлГУ, ФИТР, ПИн-119; " +
                          "2023 г."
            });
        }
    }
}
