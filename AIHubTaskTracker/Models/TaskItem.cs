using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AIHubTaskTracker.Models
{
    /// <summary>
    /// Bảng Task (Quản lý Công việc)
    /// Lưu thông tin công việc, người giao, người nhận, tiến độ, deadline...
    /// </summary>
    public class TaskItem
    {
        [Key]
        public int task_id { get; set; } //  Khóa chính 

        [Required, MaxLength(255)]
        public string title { get; set; } = null!; //  Tên công việc (VD: "Cấu hình môi trường dev")

        public string? description { get; set; } //  Mô tả chi tiết nội dung công việc

        [ForeignKey("assigner")]
        public int? assigner_id { get; set; } //  Người giao việc (FK -> Member.user_id)
        [JsonIgnore]
        public Member? assigner { get; set; }

        [ForeignKey("assignee")]
        public int? assignee_id { get; set; } //  Người thực hiện chính (FK -> Member.user_id)
        [JsonIgnore]
        public Member? assignee { get; set; }

        public List<int>? collaborators { get; set; } = new(); //  Danh sách người phối hợp (List user_id, JSON/ARRAY)

        public string? expected_output { get; set; } //  Kết quả mong đợi (VD: "API chạy local ổn định")

        public DateTime? deadline { get; set; } //  Deadline công việc

        [MaxLength(50)]
        public string? status { get; set; } //  Trạng thái: To Do, In Progress, Done, Overdue

        [Range(0, 100)]
        public int progress_percentage { get; set; } = 0; //  Tiến độ hoàn thành (0–100%)

        public DateTime created_at { get; set; } = DateTime.Now; //  Ngày tạo
        public DateTime updated_at { get; set; } = DateTime.Now; //  Ngày cập nhật gần nhất

        [MaxLength(255)]
        public string? notion_link { get; set; } //  Liên kết Notion 

        [JsonIgnore]
        public ICollection<Log> logs { get; set; } = new List<Log>(); //  Nhật ký hành động (liên kết Log)
        public string? clickup_id { get; set; }
    }
}
