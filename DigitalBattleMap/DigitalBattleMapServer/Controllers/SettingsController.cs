using System.Text.Json;
using AutoMapper;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Handlers;
using DigitalBattleMapServer.Models;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBattleMapServer.Controllers;

public class SettingsController : Controller
{
    private readonly IState<Settings> _settingsState;
    private readonly IMapper _mapper;

    public SettingsController(IState<Settings> settingsState, IMapper mapper)
    {
        _settingsState = settingsState;
        _mapper = mapper;
    }

    [HttpGet]
    [Route("Settings")]
    public IActionResult Index()
    {
        Settings settings = _settingsState.Get();
        SettingsViewModel viewModel = _mapper.Map<SettingsViewModel>(settings) ?? new SettingsViewModel();
        
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult Save(SettingsViewModel viewModel)
    {
        Settings settings = _mapper.Map<Settings>(viewModel);
        _settingsState.Set(settings);
        
        return RedirectToAction(nameof(Index));
    }
}