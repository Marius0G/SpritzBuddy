using System.Threading.Tasks;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
 public interface IProfileService
 {
 Task<bool> UpdateProfileAsync(string userId, EditProfileViewModel model);
 }
}
