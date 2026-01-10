using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Group>> GetAllGroupsAsync()
        {
            return await _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Moderator)
                .ToListAsync();
        }

        public async Task<List<Group>> GetUserGroupsAsync(int userId)
        {
            return await _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Moderator)
                .Include(g => g.Events)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.IsAccepted))
                .ToListAsync();
        }

        public async Task<Group> GetGroupWithMembersAndMessagesAsync(int groupId)
        {
            return await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Messages)
                    .ThenInclude(m => m.User)
                .Include(g => g.Moderator)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }

        public async Task<GroupMessage> GetMessageForEditAsync(int messageId, int userId)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null || message.UserId != userId)
                return null;
            return message;
        }

        public async Task<int?> GetGroupIdForMessageAsync(int messageId)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            return message?.GroupId;
        }

        public async Task<int> CreateGroupAsync(int userId, CreateGroupViewModel model)
        {
            var group = new Group
            {
                Name = model.Name,
                Description = model.Description,
                ModeratorId = userId,
                CreationDate = DateTime.Now
            };

            var membership = new UserGroup
            {
                UserId = userId,
                Group = group,
                IsAccepted = true,
                JoinedDate = DateTime.Now
            };

            group.Members.Add(membership);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return group.Id;
        }
        public async Task<bool> JoinGroupAsync(int userId, int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return false;

            bool alreadyRequested = await _context.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
            if (alreadyRequested)
                return false;

            var membership = new UserGroup
            {
                UserId = userId,
                GroupId = groupId,
                IsAccepted = false,
                JoinedDate = DateTime.Now
            };

            _context.UserGroups.Add(membership);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LeaveGroupAsync(int userId, int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            
            if (group == null)
                return false;

            // Dacă utilizatorul este moderator
            if (group.ModeratorId == userId)
            {
                // Găsește alți membri acceptați (exclude moderatorul curent)
                var otherMembers = group.Members?
                    .Where(m => m.IsAccepted && m.UserId != userId)
                    .ToList();

                if (otherMembers != null && otherMembers.Any())
                {
                    // Alege un membru aleatoriu să devină moderator
                    var random = new Random();
                    var newModerator = otherMembers[random.Next(otherMembers.Count)];
                    group.ModeratorId = newModerator.UserId;
                    
                    // Șterge vechiul moderator din membri
                    var oldModeratorMembership = group.Members?.FirstOrDefault(m => m.UserId == userId);
                    if (oldModeratorMembership != null)
                    {
                        _context.UserGroups.Remove(oldModeratorMembership);
                    }
                }
                else
                {
                    // Dacă nu mai sunt alți membri, șterge grupul
                    _context.Groups.Remove(group);
                }
            }
            else
            {
                // Dacă nu e moderator, doar îl scoatem din grup
                var membership = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
                if (membership != null)
                {
                    _context.UserGroups.Remove(membership);
                }
                else
                {
                    return false;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<Group> GetGroupForModeratorAsync(int groupId, int currentModeratorId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.ModeratorId != currentModeratorId)
                throw new UnauthorizedAccessException();
            return group;
        }

        public async Task AcceptMemberAsync(int groupId, int userId, int currentModeratorId)
        {
            await GetGroupForModeratorAsync(groupId, currentModeratorId);
            var membership = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
            if (membership != null)
            {
                membership.IsAccepted = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveMemberAsync(int groupId, int userId, int currentModeratorId)
        {
            await GetGroupForModeratorAsync(groupId, currentModeratorId);
            var membership = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
            if (membership != null)
            {
                _context.UserGroups.Remove(membership);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteGroupAsync(int groupId, int currentModeratorId)
        {
            var group = await GetGroupForModeratorAsync(groupId, currentModeratorId);
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteGroupAsAdminAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
        }
        public async Task PostMessageAsync(int groupId, int userId, string content)
        {
            var isMember = await _context.UserGroups.AnyAsync(ug => ug.GroupId == groupId && ug.UserId == userId && ug.IsAccepted);
            if (!isMember)
                throw new UnauthorizedAccessException();

            var message = new GroupMessage
            {
                GroupId = groupId,
                UserId = userId,
                Content = content,
                SentDate = DateTime.Now
            };
            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task EditMessageAsync(int messageId, int userId, string newContent)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null || message.UserId != userId)
                throw new UnauthorizedAccessException();
            message.Content = newContent;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.GroupMessages.Include(m => m.Group).FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
                return;
            if (message.UserId != userId && message.Group.ModeratorId != userId)
                throw new UnauthorizedAccessException();
            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();
        }

        // Admin methods for group messages
        public async Task<GroupMessage?> GetMessageByIdAsync(int messageId)
        {
            return await _context.GroupMessages.FindAsync(messageId);
        }

        public async Task EditMessageAsAdminAsync(int messageId, string newContent)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null)
                throw new InvalidOperationException("Message not found");
            message.Content = newContent;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMessageAsAdminAsync(int messageId)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null)
                return;
            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();
        }

        // Invite functionality
        public async Task<bool> SendInviteAsync(int groupId, int inviterId, int invitedUserId)
        {
            // Verifică dacă inviter-ul este moderator
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.ModeratorId != inviterId)
                return false;

            // Verifică dacă utilizatorul nu este deja membru sau nu are deja un invite
            var alreadyMember = await _context.UserGroups.AnyAsync(ug => ug.GroupId == groupId && ug.UserId == invitedUserId);
            if (alreadyMember)
                return false;

            var existingInvite = await _context.GroupInvites.AnyAsync(gi => 
                gi.GroupId == groupId && 
                gi.InvitedUserId == invitedUserId && 
                !gi.IsDeclined);
            if (existingInvite)
                return false;

            var invite = new GroupInvite
            {
                GroupId = groupId,
                InviterId = inviterId,
                InvitedUserId = invitedUserId,
                InvitedDate = DateTime.Now
            };

            _context.GroupInvites.Add(invite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GroupInvite>> GetPendingInvitesForUserAsync(int userId)
        {
            return await _context.GroupInvites
                .Include(gi => gi.Group)
                .Include(gi => gi.Inviter)
                .Where(gi => gi.InvitedUserId == userId && !gi.IsAccepted && !gi.IsDeclined)
                .ToListAsync();
        }

        public async Task<List<int>> GetPendingInvitesForGroupAsync(int groupId)
        {
            return await _context.GroupInvites
                .Where(gi => gi.GroupId == groupId && !gi.IsAccepted && !gi.IsDeclined)
                .Select(gi => gi.InvitedUserId)
                .ToListAsync();
        }

        public async Task AcceptInviteAsync(int inviteId, int userId)
        {
            var invite = await _context.GroupInvites.FindAsync(inviteId);
            if (invite == null || invite.InvitedUserId != userId)
                throw new UnauthorizedAccessException();

            invite.IsAccepted = true;

            // Adaugă utilizatorul în grup
            var membership = new UserGroup
            {
                UserId = userId,
                GroupId = invite.GroupId,
                IsAccepted = true,
                JoinedDate = DateTime.Now
            };

            _context.UserGroups.Add(membership);
            await _context.SaveChangesAsync();
        }

        public async Task DeclineInviteAsync(int inviteId, int userId)
        {
            var invite = await _context.GroupInvites.FindAsync(inviteId);
            if (invite == null || invite.InvitedUserId != userId)
                throw new UnauthorizedAccessException();

            invite.IsDeclined = true;
            await _context.SaveChangesAsync();
        }

        // Event functionality
        public async Task<int> CreateEventAsync(int groupId, int organizerId, string title, string description, DateTime eventDate, string? location)
        {
            // Verify user is a member of the group
            var isMember = await _context.UserGroups.AnyAsync(ug => 
                ug.GroupId == groupId && 
                ug.UserId == organizerId && 
                ug.IsAccepted);
            
            if (!isMember)
                throw new UnauthorizedAccessException("Only group members can create events.");

            var newEvent = new Event
            {
                GroupId = groupId,
                OrganizerId = organizerId,
                Title = title,
                Description = description,
                EventDate = eventDate,
                Location = location
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent.Id;
        }

        public async Task<List<Event>> GetGroupEventsAsync(int groupId)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.GroupId == groupId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event?> GetEventDetailsAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Group)
                .Include(e => e.Organizer)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        public async Task<bool> RespondToEventAsync(int eventId, int userId, EventParticipantStatus status)
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null)
                return false;

            // Verify user is a member of the group
            var isMember = await _context.UserGroups.AnyAsync(ug => 
                ug.GroupId == evt.GroupId && 
                ug.UserId == userId && 
                ug.IsAccepted);
            
            if (!isMember)
                return false;

            // Check if user already responded
            var existingResponse = await _context.EventParticipants
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

            if (existingResponse != null)
            {
                existingResponse.Status = status;
            }
            else
            {
                var participant = new EventParticipant
                {
                    EventId = eventId,
                    UserId = userId,
                    Status = status
                };
                _context.EventParticipants.Add(participant);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteEventAsync(int eventId, int userId, bool isAdmin = false)
        {
            var evt = await _context.Events
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.Id == eventId);
            
            if (evt == null)
                return;

            // Only organizer or group moderator can delete, OR admin
            if (!isAdmin && evt.OrganizerId != userId && evt.Group.ModeratorId != userId)
                throw new UnauthorizedAccessException("Only the event organizer or group moderator can delete this event.");

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(int organizerId)
        {
            return await _context.Events
                .Include(e => e.Group)
                .Include(e => e.Organizer)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsUserIsAttendingAsync(int userId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Group)
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Organizer)
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Participants)
                        .ThenInclude(p => p.User)
                .Where(ep => ep.UserId == userId && ep.Status == EventParticipantStatus.Going)
                .Select(ep => ep.Event)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }
    }
}
