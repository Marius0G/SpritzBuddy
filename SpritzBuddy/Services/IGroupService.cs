using System.Threading.Tasks;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
    public interface IGroupService
    {
        Task<int> CreateGroupAsync(int userId, CreateGroupViewModel model);
        Task UpdateGroupAsync(int groupId, string name, string description);
        Task<List<Group>> GetAllGroupsAsync();
        Task<List<Group>> GetUserGroupsAsync(int userId);
        Task<bool> JoinGroupAsync(int userId, int groupId);
        Task<bool> LeaveGroupAsync(int userId, int groupId);
        Task AcceptMemberAsync(int groupId, int userId, int currentModeratorId);
        Task RemoveMemberAsync(int groupId, int userId, int currentModeratorId);
        Task DeleteGroupAsync(int groupId, int currentModeratorId);
        Task DeleteGroupAsAdminAsync(int groupId);
        Task PostMessageAsync(int groupId, int userId, string content);
        Task EditMessageAsync(int messageId, int userId, string newContent);
        Task DeleteMessageAsync(int messageId, int userId);
        Task<GroupMessage> GetMessageForEditAsync(int messageId, int userId);
        Task<int?> GetGroupIdForMessageAsync(int messageId);
        Task<Group> GetGroupWithMembersAndMessagesAsync(int groupId);
        
        // Admin methods
        Task<GroupMessage?> GetMessageByIdAsync(int messageId);
        Task EditMessageAsAdminAsync(int messageId, string newContent);
        Task DeleteMessageAsAdminAsync(int messageId);
        
        // Invite functionality
        Task<bool> SendInviteAsync(int groupId, int inviterId, int invitedUserId);
        Task<List<GroupInvite>> GetPendingInvitesForUserAsync(int userId);
        Task<List<int>> GetPendingInvitesForGroupAsync(int groupId);
        Task AcceptInviteAsync(int inviteId, int userId);
        Task DeclineInviteAsync(int inviteId, int userId);

        // Event functionality
        Task<int> CreateEventAsync(int groupId, int organizerId, string title, string description, DateTime eventDate, string? location);
        Task<List<Event>> GetGroupEventsAsync(int groupId);
        Task<Event?> GetEventDetailsAsync(int eventId);
        Task<bool> RespondToEventAsync(int eventId, int userId, EventParticipantStatus status);
        Task UpdateEventAsync(Event evt);
        Task DeleteEventAsync(int eventId, int userId, bool isAdmin = false);
        Task<List<Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<List<Event>> GetEventsUserIsAttendingAsync(int userId);
    }
}
