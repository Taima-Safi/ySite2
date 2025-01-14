﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ySite.Core.Dtos.Replays;
using ySite.Core.StaticUserRoles;
using ySite.Service.Interfaces;

namespace ySite.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReplaysController : BaseController
{
    private readonly IReplayService _replayService;

    public ReplaysController(IReplayService replayService)
    {
        _replayService = replayService;
    }



    [HttpGet("getReplaysOnComment")]
    public async Task<IActionResult> getReplaysOnCommentasync(int commentId)
    {
        //var userid = getuserid();
        return Ok(await _replayService.getReplaysOnComment(commentId));
    }


    [HttpPost]
    public async Task<IActionResult> AddReplayAsync([FromForm] ReplayDto dto)
    {
        var userId = GetUserId();
        return Ok(await _replayService.AddReplay(dto, userId));
    }

    [HttpPatch]
    [Authorize(Policy = Policies.EditReplayPolicy)]
    public async Task<IActionResult> EditReplayAsync([FromForm] EditReplayDto dto, int replayId)
    {
        var userId = GetUserId();
        return Ok(await _replayService.EditReplay(dto, userId));
    }


    [HttpDelete]
    [Authorize(Policy = Policies.DeleteReplayPolicy)]
    public async Task<IActionResult> DeleteReplayAsync(int replayId)
    {
        var userId = GetUserId();
        return Ok(await _replayService.DeleteReplay(replayId, userId));
    }
}
