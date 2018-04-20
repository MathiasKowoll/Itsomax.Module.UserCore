using System;

namespace Itsomax.Module.UserCore.ViewModels
{
    public class UserListViewModel
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime Updated { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
    }
}