//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Models.HubAdo
{
    using System;
    using System.Collections.Generic;
    
    public partial class NotificationMessages
    {
        public System.Guid recid { get; set; }
        public int receiverUserID { get; set; }
        public int senderUserId { get; set; }
        public Nullable<int> isDeletedUser { get; set; }
        public string summaryMessage { get; set; }
        public string receiverProfileImage { get; set; }
        public string senderProfileImage { get; set; }
        public string receiverNickName { get; set; }
        public string senderNickName { get; set; }
        public bool isRead { get; set; }
        public bool isNotification { get; set; }
        public Nullable<bool> isImage { get; set; }
        public System.DateTime createdDate { get; set; }
    }
}
