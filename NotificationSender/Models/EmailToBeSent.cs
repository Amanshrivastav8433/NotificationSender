using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationSender.Models
{
    [Table("st_email_to_be_sent")]
    public class EmailToBeSent
    {
        [Key]
        [Column("pk_email_to_be_sent_id")]
        public int PkEmailToBeSentId { get; set; }

        [Required]
        [Column("from_address")]
        [MaxLength(250)]
        public string FromAddress { get; set; } = string.Empty;

        [Required]
        [Column("to_address")]
        [MaxLength(250)]
        public string ToAddress { get; set; } = string.Empty;

        [Column("cc_address")]
        [MaxLength(250)]
        public string? CcAddress { get; set; }

        [Column("bcc_address")]
        [MaxLength(250)]
        public string? BccAddress { get; set; }

        [Column("subject")]
        [MaxLength(500)]
        public string? Subject { get; set; }

        // Complete HTML will be stored here
        [Column("body")]
        public string? Body { get; set; }

        [Column("attachment_file_path")]
        [MaxLength(1000)]
        public string? AttachmentFilePath { get; set; }

        [Column("attachment_count")]
        public int AttachmentCount { get; set; }

        // 1 = Pending
        // 2 = Sent
        // 3 = Failed
        [Column("status")]
        public byte Status { get; set; } = EmailStatus.Pending;

        [Column("sent_date")]
        public DateTime? SentDate { get; set; }

        [Column("attempt_count")]
        public int AttemptCount { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }
    }

    public static class EmailStatus
    {
        public const byte Pending = 1;
        public const byte Sent = 2;
        public const byte Failed = 3;
    }
}