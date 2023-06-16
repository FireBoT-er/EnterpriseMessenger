using EnterpriseMessengerServer.Hubs;
using EnterpriseMessengerServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace EnterpriseMessengerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment appEnvironment;
        private readonly DBContext context;
        private readonly IHubContext<MessengerHub> hubContext;

        public FilesController(UserManager<ApplicationUser> userManager, IWebHostEnvironment appEnvironment, DBContext context, IHubContext<MessengerHub> hubContext)
        {
            this.userManager = userManager;
            this.appEnvironment = appEnvironment;
            this.context = context;
            this.hubContext = hubContext;
        }

        [HttpPost]
        [Route("changeAvatar")]
        public async Task<IActionResult> ChangeAvatar(IFormFile uploadedFile)
        {
            if (uploadedFile != null)
            {
                var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == HttpContext.User.Identity!.Name!).First();
                string path = "/UserFiles/Avatars/" + user!.Id + ".png";
                using (var fileStream = new FileStream(appEnvironment.ContentRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                user.HasPhoto = true;
                context.SaveChanges();
                
                var ids = user.SentMessages.Concat(user.ReceivedMessages).OrderByDescending(m => m.SendDateTime).Select(m => m.AuthorId == user.Id ? m.ReceiverUserId : m.AuthorId).ToList();
                ids = ids.Distinct().ToList();

                if (ids.Any())
                {
                    await hubContext.Clients.Users(userManager.Users.Where(u => ids.Contains(u.Id)).Select(u => u.UserName)!).SendAsync("SendUserUpdatedPhoto", user.Id);
                }

                return Ok(path);
            }

            return BadRequest();
        }

        [HttpPost]
        [Route("changeGroupChatAvatar")]
        public async Task<IActionResult> ChangeGroupChatAvatar(IFormFile uploadedFile)
        {
            if (uploadedFile != null)
            {
                var chat = context.GroupChats.Where(gp => gp.Id == uploadedFile.FileName).First();
                string path = "/UserFiles/GroupChatAvatars/" + uploadedFile.FileName + ".png";
                using (var fileStream = new FileStream(appEnvironment.ContentRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                chat.HasPhoto = true;
                context.SaveChanges();

                await hubContext.Clients.Group(uploadedFile.FileName).SendAsync("SendGroupChatUpdatedPhoto", uploadedFile.FileName);

                return Ok(path);
            }

            return BadRequest();
        }

        [HttpPost]
        [Route("sendFile")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> SendFile()
        {
            var reader = new MultipartReader(HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value!, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    try
                    {
                        using var fileStream = new FileStream(appEnvironment.ContentRootPath + "/UserFiles/Attachments/" + WebUtility.HtmlEncode(contentDisposition!.FileName.Value), FileMode.Create);
                        await section.Body.CopyToAsync(fileStream);
                    }
                    catch
                    {
                        return StatusCode(StatusCodes.Status507InsufficientStorage);
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Ok();
        }
    }
}
