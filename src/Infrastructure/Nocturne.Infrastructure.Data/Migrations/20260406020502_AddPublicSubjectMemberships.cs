using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nocturne.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSubjectMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert TenantMember for Public subject on each existing tenant that doesn't already have one
            migrationBuilder.Sql("""
                INSERT INTO tenant_members (id, tenant_id, subject_id, limit_to_24_hours, label, sys_created_at, sys_updated_at)
                SELECT gen_random_uuid(), t.id, s.id, false, 'Public Access', now(), now()
                FROM tenants t
                CROSS JOIN subjects s
                WHERE s.is_system_subject = true AND s.name = 'Public'
                AND NOT EXISTS (
                    SELECT 1 FROM tenant_members tm WHERE tm.tenant_id = t.id AND tm.subject_id = s.id
                );
                """);

            // Assign readable role to each new Public subject membership that doesn't already have a role
            migrationBuilder.Sql("""
                INSERT INTO tenant_member_roles (id, tenant_member_id, tenant_role_id, sys_created_at)
                SELECT gen_random_uuid(), tm.id, tr.id, now()
                FROM tenant_members tm
                JOIN subjects s ON s.id = tm.subject_id AND s.is_system_subject = true AND s.name = 'Public'
                JOIN tenant_roles tr ON tr.tenant_id = tm.tenant_id AND tr.slug = 'readable'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_member_roles tmr WHERE tmr.tenant_member_id = tm.id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove role assignments for Public subject memberships
            migrationBuilder.Sql("""
                DELETE FROM tenant_member_roles WHERE tenant_member_id IN (
                    SELECT tm.id FROM tenant_members tm
                    JOIN subjects s ON s.id = tm.subject_id AND s.is_system_subject = true AND s.name = 'Public'
                );
                """);

            // Remove Public subject memberships
            migrationBuilder.Sql("""
                DELETE FROM tenant_members WHERE subject_id IN (
                    SELECT id FROM subjects WHERE is_system_subject = true AND name = 'Public'
                );
                """);
        }
    }
}
