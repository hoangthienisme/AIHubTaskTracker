using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIHubTaskTracker.Data;
using AIHubTaskTracker.Models;
using AIHubTaskTracker.DTOs;

namespace AIHubTaskTracker.Controllers
{
	[ApiController]
	[Route("api/v1/[controller]")] // => api/v1/users
	public class UsersController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly ILogger<UsersController> _logger;

		public UsersController(AppDbContext context, ILogger<UsersController> logger)
		{
			_context = context;
			_logger = logger;
		}

		// GET: api/v1/users
		[HttpGet]
		public async Task<IActionResult> GetAll(
			[FromQuery] string? clickup_id = null,
			[FromQuery] string? email = null)
		{
			try
			{
				_logger.LogInformation($"📥 [USERS] GET /api/v1/users - clickup_id={clickup_id}, email={email}");

				// Filter by clickup_id
				if (!string.IsNullOrEmpty(clickup_id))
				{
					var userByClickUp = await _context.Members
						.Where(m => m.clickup_id == clickup_id)
						.Select(m => new
						{
							id = m.user_id,
							user_id = m.user_id,
							clickup_id = m.clickup_id,
							full_name = m.full_name,
							name = m.full_name,
							email = m.email,
							role = m.role.ToString(),
							position = m.position.ToString(),
							avatar_url = m.avatar_url,
							status = m.status.ToString()
						})
						.ToListAsync();

					if (userByClickUp.Count == 0)
					{
						_logger.LogWarning($"⚠️ [USERS] No user found with clickup_id={clickup_id}");
						return NotFound(new { error = "User not found" });
					}

					_logger.LogInformation($"✅ [USERS] Found {userByClickUp.Count} user(s) with clickup_id={clickup_id}");
					return Ok(userByClickUp);
				}

				// Filter by email
				if (!string.IsNullOrEmpty(email))
				{
					var userByEmail = await _context.Members
						.Where(m => m.email == email)
						.Select(m => new
						{
							id = m.user_id,
							user_id = m.user_id,
							clickup_id = m.clickup_id,
							full_name = m.full_name,
							name = m.full_name,
							email = m.email,
							role = m.role.ToString(),
							position = m.position.ToString(),
							avatar_url = m.avatar_url,
							status = m.status.ToString()
						})
						.ToListAsync();

					if (userByEmail.Count == 0)
					{
						_logger.LogWarning($"⚠️ [USERS] No user found with email={email}");
						return NotFound(new { error = "User not found" });
					}

					_logger.LogInformation($"✅ [USERS] Found {userByEmail.Count} user(s) with email={email}");
					return Ok(userByEmail);
				}

				// Get all users
				var users = await _context.Members
					.Select(m => new
					{
						id = m.user_id,
						user_id = m.user_id,
						clickup_id = m.clickup_id,
						full_name = m.full_name,
						name = m.full_name,
						email = m.email,
						role = m.role.ToString(),
						position = m.position.ToString(),
						avatar_url = m.avatar_url,
						status = m.status.ToString()
					})
					.ToListAsync();

				_logger.LogInformation($"✅ [USERS] Returning {users.Count} users");
				return Ok(users);
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ [USERS] Error: {ex.Message}");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		// GET: api/v1/users/{id}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation($"📥 [USERS] GET /api/v1/users/{id}");

				var user = await _context.Members
					.Where(m => m.user_id == id)
					.Select(m => new
					{
						id = m.user_id,
						user_id = m.user_id,
						clickup_id = m.clickup_id,
						full_name = m.full_name,
						name = m.full_name,
						email = m.email,
						role = m.role.ToString(),
						position = m.position.ToString(),
						avatar_url = m.avatar_url,
						status = m.status.ToString()
					})
					.FirstOrDefaultAsync();

				if (user == null)
				{
					_logger.LogWarning($"⚠️ [USERS] User {id} not found");
					return NotFound(new { error = "User not found" });
				}

				_logger.LogInformation($"✅ [USERS] Found user {id}");
				return Ok(user);
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ [USERS] Error: {ex.Message}");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		// POST: api/v1/users
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] MemberCreateDto dto)
		{
			try
			{
				_logger.LogInformation($"📥 [USERS] POST /api/v1/users - Creating: {dto.full_name}");

				if (!ModelState.IsValid)
				{
					_logger.LogWarning($"⚠️ [USERS] Invalid model state");
					return BadRequest(ModelState);
				}

				// Check duplicate email
				if (await _context.Members.AnyAsync(m => m.email == dto.email))
				{
					_logger.LogWarning($"⚠️ [USERS] Email already exists: {dto.email}");
					return Conflict(new { error = "Email already exists" });
				}

				var member = new Member
				{
					full_name = dto.full_name,
					email = dto.email,
					password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password),
					role = dto.role,
					position = dto.position,
					status = Models.Enums.StatusType.Online,
					created_at = DateTime.UtcNow,
					updated_at = DateTime.UtcNow
				};

				_context.Members.Add(member);
				await _context.SaveChangesAsync();

				_logger.LogInformation($"✅ [USERS] Created user: {member.user_id} - {member.full_name}");

				return CreatedAtAction(nameof(GetById), new { id = member.user_id }, new
				{
					id = member.user_id,
					user_id = member.user_id,
					clickup_id = member.clickup_id,
					full_name = member.full_name,
					name = member.full_name,
					email = member.email,
					role = member.role.ToString(),
					position = member.position.ToString(),
					status = member.status.ToString()
				});
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ [USERS] Error creating user: {ex.Message}");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		// PUT: api/v1/users/{id}
		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] MemberUpdateDto dto)
		{
			try
			{
				_logger.LogInformation($"📥 [USERS] PUT /api/v1/users/{id}");

				var member = await _context.Members.FindAsync(id);
				if (member == null)
				{
					_logger.LogWarning($"⚠️ [USERS] User {id} not found");
					return NotFound(new { error = "User not found" });
				}

				if (!string.IsNullOrEmpty(dto.full_name)) member.full_name = dto.full_name;
				if (!string.IsNullOrEmpty(dto.email)) member.email = dto.email;
				if (!string.IsNullOrEmpty(dto.password)) member.password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password);
				member.role = dto.role;
				member.position = dto.position;
				member.updated_at = DateTime.UtcNow;

				await _context.SaveChangesAsync();

				_logger.LogInformation($"✅ [USERS] Updated user {id}");
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ [USERS] Error updating user: {ex.Message}");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		// DELETE: api/v1/users/{id}
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				_logger.LogInformation($"📥 [USERS] DELETE /api/v1/users/{id}");

				var member = await _context.Members.FindAsync(id);
				if (member == null)
				{
					_logger.LogWarning($"⚠️ [USERS] User {id} not found");
					return NotFound(new { error = "User not found" });
				}

				_context.Members.Remove(member);
				await _context.SaveChangesAsync();

				_logger.LogInformation($"✅ [USERS] Deleted user {id}");
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ [USERS] Error deleting user: {ex.Message}");
				return StatusCode(500, new { error = ex.Message });
			}
		}
	}
}