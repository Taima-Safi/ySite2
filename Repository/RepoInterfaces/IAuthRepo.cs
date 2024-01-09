﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ySite.Core.StaticUserRoles;
using ySite.EF;
using ySite.EF.Entities;

namespace Repository.RepoInterfaces
{
    public interface IAuthRepo
    {
        Task<bool> CheckPassword(ApplicationUser user, string password);
        Task<ApplicationUser> FindByName(string userName);
        Task<string> InsertUser(ApplicationUser user, string Password);
        Task<string> GenerateTokenString(ApplicationUser user, JwtConfiguration jwtConfig);
        Task<List<Claim>> GetClaims(string userName);
        List<Claim> GetClaimsSeperated(IList<Claim> claims);
        Task<bool> AddUserClaim(string user, Claim claim);
        Task GenerateCookieAuthentication(string username);
        Task<List<string>> GetUserRoles(ApplicationUser user);
        Task UpdateUser(ApplicationUser user);
        Task<ApplicationUser> UserfromToken(string token);
        Task<List<Claim>> GetRoleClaims(string rolename);
        Task<ApplicationUser> FindById(string userId);
        Task<bool> Remove(ApplicationUser user);
    }
}
