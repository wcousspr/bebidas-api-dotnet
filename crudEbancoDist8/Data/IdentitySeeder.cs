using crudEbancoDist8.Authorization;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Identity;

namespace crudEbancoDist.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                AppRoles.Admin,
                AppRoles.User
            };

            foreach (var role in roles)
            {
                var roleExists =
                    await roleManager.RoleExistsAsync(role);

                if (!roleExists)
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminAsync(
            UserManager<Usuario> userManager,
                string userId)
        {
            var usuario = await userManager.FindByIdAsync(userId);

            if (usuario is null)
            {
                throw new InvalidOperationException(
                    "Usuário configurado para administrador não encontrado.");
            }

            var jaEhAdmin = await userManager.IsInRoleAsync(
                usuario,
                AppRoles.Admin);

            if (jaEhAdmin)
            {
                return;
            }

            var result = await userManager.AddToRoleAsync(
                usuario,
                AppRoles.Admin);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Erro ao atribuir Admin: {errors}");
            }
        }


    }
}