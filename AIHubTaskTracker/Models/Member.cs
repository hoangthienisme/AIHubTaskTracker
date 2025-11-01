using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AIHubTaskTracker.Models.Enums;

namespace AIHubTaskTracker.Models
{
    /// <summary>
    /// Bảng Member (User Authentication)
    /// Lưu thông tin người dùng, dùng cho xác thực & phân quyền hệ thống
    /// </summary>
    public class Member
    {
        [Key]
        public int user_id { get; set; } //  Khóa chính 

        [Required, MaxLength(255)]
        public string full_name { get; set; } = null!; //  Tên đầy đủ 

        [Required, EmailAddress, MaxLength(255)]
        public string email { get; set; } = null!; //  Email người dùng 
        public string? avatar_url { get; set; }
        public StatusType status { get; set; } = StatusType.Online; // mặc định online


        [Required]
        public string password_hash { get; set; } = null!; //  Lưu hash mật khẩu 

        [Required]
        public RoleType role { get; set; } //  Vai trò trong hệ thống (VD: Backend_Developer, Lead_Developer, Founder)

        [Required, MaxLength(100)]
        public PositionType position { get; set; }  //  Vị trí công việc (VD: Backend Developer / API Integration Engineer)

        public DateTime created_at { get; set; } = DateTime.Now; //  Ngày tạo tài khoản
        public DateTime updated_at { get; set; } = DateTime.Now; //  Ngày cập nhật gần nhất
		[MaxLength(50)]
		public string? clickup_id { get; set; } // 🔥 THÊM dòng này

		// Navigation properties
		[JsonIgnore]
        public ICollection<TaskItem> assigned_tasks { get; set; } = new List<TaskItem>(); //  Các task được giao cho user này

        [JsonIgnore]
        public ICollection<TaskItem> created_tasks { get; set; } = new List<TaskItem>(); //  Các task do user này tạo (assigner)

        [JsonIgnore]
        public ICollection<Log> logs { get; set; } = new List<Log>(); //  Nhật ký hành động (logs của user)

        [JsonIgnore]
        public ICollection<Report> reports { get; set; } = new List<Report>(); //  Báo cáo KPI hoặc báo cáo tiến độ
    }
}
