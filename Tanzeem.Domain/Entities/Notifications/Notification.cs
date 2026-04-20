using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Notifications
{
    public class Notification
    {
        public int Id { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; }
        public NotificationType Type { get; set; }

        #region FK
        #endregion
        public int UserId { get; set; }

        #region navigation properties
        #endregion
        public User User { get; set; }
    }
}
