using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcomUserApi.Models;
using EcomUserApi.Services;
using EcomUserApi.RabbitMQ;

namespace EcomUserApi.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;
    private static readonly List<User> Users = new List<User>();
    private readonly IRabbitMQClient _rabbitMqClient;

    public UserController(UserService userService, ILogger<UserController> logger, IRabbitMQClient rabbitMQClient)
    {
        _userService = userService;
        _rabbitMqClient = rabbitMQClient;
        _logger = logger;
    }


    [HttpGet]
    public async Task<List<User>> Get() =>
        await _userService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<User>> Get(string id)
    {
        var user = await _userService.GetAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return user;
    }

    [HttpPost]
    public async Task<IActionResult> Post(User newUser)
    {
        await _userService.CreateAsync(newUser);

        var payload = JsonSerializer.Serialize(newUser);

        _logger.LogInformation($"New user created: {payload}");
        _rabbitMqClient.Publish("user.action", "user.created", payload);

        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, User updatedUser)
    {
        var user = await _userService.GetAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        updatedUser.Id = user.Id;
        // how to prevent over-posting the right way?? ¯\_(ツ)_/¯
        updatedUser.EmailAddress = user.EmailAddress;
        updatedUser.IsEnrolled = user.IsEnrolled;
        updatedUser.Title = user.Title;

        await _userService.UpdateAsync(id, updatedUser);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userService.GetAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        await _userService.RemoveAsync(id);

        return NoContent();
    }
}