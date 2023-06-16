using EnterpriseMessengerServer.Hubs;
using EnterpriseMessengerServer.Models;
using EnterpriseMessengerServer.Models.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseMessengerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly DBContext context;
        private readonly IHubContext<MessengerHub> hubContext;
        private readonly IConfigurationRoot JSONParams;

        public UsersController(UserManager<ApplicationUser> userManager, DBContext context, IHubContext<MessengerHub> hubContext)
        {
            this.userManager = userManager;
            this.context = context;
            this.hubContext = hubContext;

            JSONParams = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(Register model)
        {
            var userExists = await userManager.FindByNameAsync(model.UserName);
            if (userExists != null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Данный логин уже занят" });
            }

            ApplicationUser user = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.UserName,
                Surname = model.Surname,
                Name = model.Name,
                Patronymic = model.Patronymic,
                Position = model.Position
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await userManager.AddToRoleAsync(user, UserRoles.User);

            return Ok(new { Message = "Пользователь успешно создан" });
        }

        [HttpPost]
        [Route("changeFullNamePosition")]
        public async Task<IActionResult> ChangeFullNamePosition(ChangeFullNamePosition model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            if (!string.IsNullOrWhiteSpace(model.Surname))
            {
                user.Surname = model.Surname;
            }

            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                user.Name = model.Name;
            }

            if (!string.IsNullOrWhiteSpace(model.Patronymic))
            {
                user.Patronymic = model.Patronymic;
            }

            if (!string.IsNullOrWhiteSpace(model.Position))
            {
                user.Position = model.Position;
            }

            if (!string.IsNullOrWhiteSpace(model.Surname) || !string.IsNullOrWhiteSpace(model.Name) || !string.IsNullOrWhiteSpace(model.Patronymic) || !string.IsNullOrWhiteSpace(model.Position))
            {
                context.SaveChanges();

                await hubContext.Clients.All.SendAsync("SendUpdatedUserFullNamePosition",
                                                       model.Id,
                                                       !string.IsNullOrWhiteSpace(model.Surname) ? model.Surname : string.Empty,
                                                       !string.IsNullOrWhiteSpace(model.Name) ? model.Name : string.Empty,
                                                       !string.IsNullOrWhiteSpace(model.Patronymic) ? model.Patronymic : string.Empty,
                                                       !string.IsNullOrWhiteSpace(model.Position) ? model.Position : string.Empty);

                return Ok(new { Message = "Данные успешно обновлены" });
            }

            return BadRequest(new { Status = "Ошибка", Message = "Не переданы данные для обновления" });
        }

        [HttpPost]
        [Route("ban")]
        public async Task<IActionResult> Ban(UserIdOnly model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            if (user.HasAccess)
            {
                user.HasAccess = false;
                context.SaveChanges();

                await hubContext.Clients.All.SendAsync("SendUpdatedUserHasAccess", model.Id, false);

                return Ok(new { Message = "Пользователь успешно заблокирован" });
            }

            return BadRequest(new { Status = "Ошибка", Message = "Пользователь уже заблокирован" });
        }

        [HttpPost]
        [Route("pardon")]
        public async Task<IActionResult> Pardon(UserIdOnly model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            if (!user.HasAccess)
            {
                user.HasAccess = true;
                context.SaveChanges();

                await hubContext.Clients.All.SendAsync("SendUpdatedUserHasAccess", model.Id, true);

                return Ok(new { Message = "Пользователь успешно разблокирован" });
            }

            return BadRequest(new { Status = "Ошибка", Message = "Пользователь не был заблокирован" });
        }

        [HttpPost]
        [Route("op")]
        public async Task<IActionResult> Op(UserIdOnly model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            if (await userManager.IsInRoleAsync(user, UserRoles.User))
            {
                await userManager.RemoveFromRoleAsync(user, UserRoles.User);
                await userManager.AddToRoleAsync(user, UserRoles.Admin);

                return Ok(new { Message = "Права администратора успешно выданы" });
            }

            return BadRequest(new { Status = "Ошибка", Message = "Пользователь уже обладает правами администратора" });
        }

        [HttpPost]
        [Route("deop")]
        public async Task<IActionResult> Deop(UserIdOnly model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            if (await userManager.IsInRoleAsync(user, UserRoles.Admin))
            {
                await userManager.RemoveFromRoleAsync(user, UserRoles.Admin);
                await userManager.AddToRoleAsync(user, UserRoles.User);

                return Ok(new { Message = "Права администратора успешно отозваны" });
            }

            return BadRequest(new { Status = "Ошибка", Message = "Пользователь не обладает правами администратора" });
        }

        [HttpPost]
        [Route("changePassword")]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(HttpContext.User.Identity!.Name!);
            IdentityResult result = await userManager.ChangePasswordAsync(user!, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Пароль успешно изменён" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return BadRequest(new { Status = "Ошибка", Message = "Пользователя с данным ID не существует" });
            }

            var result = await userManager.ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user), model.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new { Message = $"Пароль успешно изменён. Новый пароль: {model.NewPassword}" });;
            }

            return BadRequest(result.Errors);
        }

        [HttpGet]
        [Route("editDeleteTimeLimitChanged")]
        public async Task<IActionResult> EditDeleteTimeLimitChanged()
        {
            await hubContext.Clients.All.SendAsync("EditDeleteTimeLimitChanged", Math.Abs(JSONParams.GetValue<int>("EditDeleteTimeLimit")));
            return Ok(new { Message = "Данные успешно отправлены пользователям" });
        }
    }
}
