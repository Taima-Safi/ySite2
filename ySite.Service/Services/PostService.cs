﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Repository.RepoInterfaces;
using ySite.Core.Dtos.Post;
using ySite.Core.Dtos.Posts;
using ySite.Core.StaticFiles;
using ySite.EF.Entities;
using ySite.Service.Interfaces;

namespace ySite.Service.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepo _postRepo;
        private readonly IAuthRepo _authRepo;
        private readonly IReactionRepo _reactionRepo;
        private readonly ICommentRepo _commentRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostingEnvironment _host;
        private readonly string _imagepath;

        public PostService(IPostRepo postRepo, IAuthRepo authRepo,
            IHttpContextAccessor httpContextAccessor, IReactionRepo reactionRepo,
            ICommentRepo commentRepo, IHostingEnvironment host)
        {
            _postRepo = postRepo;
            _authRepo = authRepo;
            _httpContextAccessor = httpContextAccessor;
            _reactionRepo = reactionRepo;
            _commentRepo = commentRepo;
            _host = host;
            _imagepath = $"{_host.WebRootPath}{FilesSettings.ImagesPath}";
        }

        public async Task<UserPostsResultDto> GetUserPosts(string userId)
        {
            var userPostsR = new UserPostsResultDto();
            if (userId is null)
            {
                userPostsR.Message = "UserId invalid!";
                return userPostsR;
            }
            var user = await _authRepo.FindById(userId);
            if(user is null)
            {
                userPostsR.Message = "User Is Not Found!";
                return userPostsR;
            }
            var posts = await _postRepo.GetPostsAsync(user);

            if (posts is null || !posts.Any())
            {
                userPostsR.Message = "This user does not have any posts";
                return userPostsR;
            }
            userPostsR.Message = $"The Posts for {user.FirstName} user are these....";
            userPostsR.Posts = posts.Select(post => new UserPostsDto
            {
                Id = post.Id,
                Description = post.Description,
                Image = post.Image,
                CommentsCount = post.CommentsCount,
                ReactsCount = post.ReactsCount,
                LikeCount = post.Reactions?.Count(r => r.Reaction == ReactionType.Like)?? 0,
                LoveCount = post.Reactions?.Count(r => r.Reaction == ReactionType.Love)?? 0,
                SadCount = post.Reactions?.Count(r => r.Reaction == ReactionType.Sad) ?? 0,
                AngryCount = post.Reactions?.Count(r => r.Reaction == ReactionType.Angry)?? 0
            }).ToList();

            return userPostsR;
        }

        public async Task<bool> AddPost(PostDto dto, string userId)
        {
            if (dto == null || userId == null || (dto.Description == null && dto.ClientFile == null))
            {
                return false;
            }
            var post = new PostModel();
            post.UserId = userId;
            if (dto.Description != null)
                post.Description = dto.Description;

            string fileName = string.Empty;
            if (dto.ClientFile != null)
            {
                string myUpload = Path.Combine(_imagepath, "postsImages");
                fileName = dto.ClientFile.FileName;
                string fullPath = Path.Combine(myUpload, fileName);

                dto.ClientFile.CopyTo(new FileStream(fullPath, FileMode.Create));
                post.Image = fileName;
            }

            if (await _postRepo.addPostAsync(post))
                return true;

            return false;
        }

        public async Task<string> EditPost(UpdatePostDto dto, string userId)
        {
            if (dto == null)
                return "";

            var post = await _postRepo.GetPostAsync(dto.Id);
            if (post == null)
                return "Invalid Post";

            if (dto.Description != null)
                post.Description = dto.Description;

            string fileName = string.Empty;
            if (dto.ClientFile != null)
            {
                string myUpload = Path.Combine(_imagepath, "postsImages");
                fileName = dto.ClientFile.FileName;
                string fullPath = Path.Combine(myUpload, fileName);

                dto.ClientFile.CopyTo(new FileStream(fullPath, FileMode.Create));
                post.Image = fileName;
            }
            post.UpdatedOn = DateTime.UtcNow;
            _postRepo.updatePost(post);

            return "Post Updated...";
        }

        public async Task<string> DeletePost(int postId, string userId)
        {
            var user =await _authRepo.FindById(userId);
            if (user is null)
                return "Invalid User";
            
            var post =await _postRepo.GetPostAsync(postId);
            if (post is null)
                return "Invalid Post";
            var reactions = await _reactionRepo.GetReactionsOnPost(postId);
            var comments = await _commentRepo.GetCommentsOnPost(postId);
            post.IsDeleted = true;
            post.DeletedOn = DateTime.UtcNow;

            foreach (var reaction in reactions)
            {
                reaction.IsDeleted = true;
                reaction.DeletedOn = DateTime.UtcNow;
            }
            foreach (var comment in comments)
            {
                comment.IsDeleted = true;
                comment.DeletedOn = DateTime.UtcNow;
            }
            
            _postRepo.updatePost(post);

            return "this post deleted ...";
        }
    }
}