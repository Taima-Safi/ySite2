﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;
using Repository.RepoInterfaces;
using ySite.Core.Dtos.Comments;
using ySite.Core.Helper;
using ySite.Core.StaticFiles;
using ySite.Core.StaticUserRoles;
using ySite.EF.Entities;
using ySite.Service.Interfaces;

namespace ySite.Service.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepo _commentRepo;
    private readonly IPostRepo _postRepo;
    private readonly IReplayRepo _replayRepo;
    private readonly IAuthRepo _authRepo;
    private readonly IHostingEnvironment _host;
    private readonly string _imagepath;
    private readonly string _videoPath;
    private readonly IStringLocalizer<CommentService> _localizer;

    public CommentService(ICommentRepo commentRepo, IPostRepo postRepo,
        IAuthRepo authRepo, IReplayRepo replayRepo, IHostingEnvironment host,
        IStringLocalizer<CommentService> localizer)
    {
        _commentRepo = commentRepo;
        _postRepo = postRepo;
        _authRepo = authRepo;
        _replayRepo = replayRepo;
        _host = host;
        _imagepath = $"{_host.WebRootPath}{FilesSettings.ImagesPath}";
        _videoPath = $"{_host.WebRootPath}{FilesSettings.VideosPath}";
        _localizer = localizer;
    }

    public async Task<UserCommentsResultDto> GetUserComments(string userId)
    {
        var userCommentsR = new UserCommentsResultDto();
        var user = await _authRepo.FindById(userId);
        if (user is null)
        {
            userCommentsR.Message = _localizer[SharedResources.InvalidUser];
            return userCommentsR;
        }
        var comments = await _commentRepo.GetCommentsAsync(user);

        if (comments is null || !comments.Any())
        {
            userCommentsR.Message = $"{_localizer[SharedResources.NoComments]} from {user.FirstName} ...";
            return userCommentsR;
        }
        userCommentsR.Comments = comments.Select(c => new ReadCommentDto
        {
            Message = $"Comments of {c.User.FirstName} : ",
            Comment = c.Comment,
            PostId = c.PostId,
            Image = c.File,
            CreatedOn = c.CreatedOn,
            UserId = c.UserId,
            Id = c.Id,
            ReplaisCount = c.RepliesCount,
        }).ToList();

        return userCommentsR;
    }

    public async Task<UserCommentsResultDto> GetCommentsOnPost(int postId)
    {
        var commentR = new UserCommentsResultDto();
        var post = await _postRepo.GetPostAsync(postId);
        if (post is null)
        {
            commentR.Message = _localizer[SharedResources.InvalidPost];
            return commentR;
        }
        var comments = await _commentRepo.GetCommentsOnPost(postId);
        //if(comments == null)
        if (!comments.Any())
        {
            commentR.Message = "No Comments on this post";
            return commentR;
        }
        commentR.Message = $"This is All Comments on {post.Id}";
        commentR.Comments = comments.Select(c => new ReadCommentDto
        {
            Id = c.Id,
            Comment = c.Comment,
            Image = c.File,
            PostId = c.PostId,
            UserId = c.UserId,
            CreatedOn = c.CreatedOn,
            ReplaisCount = c.RepliesCount,

        }).ToList();
        return commentR;
    }

    public async Task<ReadCommentDto> AddComment(CommentDto dto, string userId)
    {
        var commentR = new ReadCommentDto();
        var user = await _authRepo.FindById(userId);
        if (user is null)
        {
            commentR.Message = _localizer[SharedResources.InvalidUser];
            return commentR;
        }

        var post = await _postRepo.GetPostAsync(dto.PostId);
        if (post is null)
        {
            commentR.Message = _localizer[SharedResources.InvalidPost];
            return commentR;
        }
        if (dto == null || (dto.Comment == null && dto.ClientFile == null))
        {
            commentR.Message = "Can not add Empty comment ";
            return commentR;
        }
        var comment = new CommentModel();
        if (dto.Comment is not null)
            comment.Comment = dto.Comment;

        comment.PostId = dto.PostId;
        comment.UserId = userId;
        comment.CreatedOn = DateTime.Now;

        string fileName = string.Empty;
        if (dto.ClientFile != null)
        {
            string myUpload;
            var result = FilesSettings.AllowUplaod(dto.ClientFile);
            if (result.IsValid)
            {
                if (result.Message == "video")
                    myUpload = Path.Combine(_videoPath, "commentsVideos");
                else
                    myUpload = Path.Combine(_imagepath, "commentsImages");
                fileName = dto.ClientFile.FileName;
                string fullPath = Path.Combine(myUpload, fileName);

                dto.ClientFile.CopyTo(new FileStream(fullPath, FileMode.Create));
                comment.File = fileName;
            }
            else
            {
                commentR.Message = result.Message;
                return commentR;
            }
        }
        var commented = await _commentRepo.addCommentAsync(comment);
        if (commented != null)
        {
            commentR.Message = _localizer[SharedResources.Added];
            commentR.Id = commented.Id;
            commentR.PostId = commented.PostId;
            commentR.UserId = userId;
            commentR.Comment = commented.Comment;
            commentR.CreatedOn = commented.CreatedOn;
            commentR.Image = commented.File;
            commentR.ReplaisCount = commented.RepliesCount;

            post.CommentsCount += 1;
            _postRepo.updatePost(post);

            return commentR;
        }
        commentR.Message = "Can not Comment!..";
        return commentR;
    }

    public async Task<string> DeleteComment(int commentId, string userId)
    {
        var user = await _authRepo.FindById(userId);
        if (user is null)
            return _localizer[SharedResources.InvalidUser];

        var comment = await _commentRepo.GetCommentAsync(commentId);
        if (comment is null)
            return _localizer[SharedResources.NoComments];

        var commentPost = await _postRepo.GetPostAsync(comment.PostId);
        var userRoles = await _authRepo.GetUserRoles(user);

        if (comment.UserId != userId && commentPost.UserId != userId && !userRoles.Contains(UserRoles.OWNER))
            return _localizer[SharedResources.NoPermission];

        var replies = await _replayRepo.GetReplaysOnComment(commentId);
        if (replies is not null)
        {
            foreach (var replay in replies)
            {
                replay.IsDeleted = true;
                replay.DeletedOn = DateTime.UtcNow;
                _replayRepo.updateReplay(replay);
            }
        }
        var post = await _postRepo.GetPostAsync(comment.PostId);
        post.CommentsCount -= 1;

        comment.IsDeleted = true;
        comment.DeletedOn = DateTime.UtcNow;
        _commentRepo.updateComment(comment);

        _postRepo.updatePost(post);

        return _localizer[SharedResources.Deleted];
    }

}
