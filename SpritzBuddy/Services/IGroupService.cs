using System.Threading.Tasks;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
    public interface IGroupService
    {
        Task<int> CreateGroupAsync(int userId, CreateGroupViewModel model);
        Task<List<Group>> GetAllGroupsAsync();
        Task<List<Group>> GetUserGroupsAsync(int userId);
        Task<bool> JoinGroupAsync(int userId, int groupId);
        Task<bool> LeaveGroupAsync(int userId, int groupId);
        Task AcceptMemberAsync(int groupId, int userId, int currentModeratorId);
        Task RemoveMemberAsync(int groupId, int userId, int currentModeratorId);
        Task DeleteGroupAsync(int groupId, int currentModeratorId);
        Task PostMessageAsync(int groupId, int userId, string content);
        Task EditMessageAsync(int messageId, int userId, string newContent);
        Task DeleteMessageAsync(int messageId, int userId);
        Task<GroupMessage> GetMessageForEditAsync(int messageId, int userId);
        Task<int?> GetGroupIdForMessageAsync(int messageId);
        Task<Group> GetGroupWithMembersAndMessagesAsync(int groupId);
        
        // Invite functionality
        Task<bool> SendInviteAsync(int groupId, int inviterId, int invitedUserId);
        Task<List<GroupInvite>> GetPendingInvitesForUserAsync(int userId);
        Task<List<int>> GetPendingInvitesForGroupAsync(int groupId);
        Task AcceptInviteAsync(int inviteId, int userId);
        Task DeclineInviteAsync(int inviteId, int userId);
        
        // Event functionality
        Task<int> CreateEventAsync(int groupId, int creatorId, string title, string description, DateTime eventDate, string? location);
        Task<List<GroupEvent>> GetGroupEventsAsync(int groupId);
        Task<GroupEvent?> GetEventDetailsAsync(int eventId);
        Task<bool> RespondToEventAsync(int eventId, int userId, EventParticipationStatus status);
        Task DeleteEventAsync(int eventId, int userId);
    }
}
