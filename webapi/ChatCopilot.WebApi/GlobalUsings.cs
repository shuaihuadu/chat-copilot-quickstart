﻿global using Azure.Identity;
global using ChatCopilot.Shared;
global using ChatCopilot.WebApi.Attributes;
global using ChatCopilot.WebApi.Auth;
global using ChatCopilot.WebApi.Controllers;
global using ChatCopilot.WebApi.Extensions;
global using ChatCopilot.WebApi.Hubs;
global using ChatCopilot.WebApi.Models.Request;
global using ChatCopilot.WebApi.Models.Response;
global using ChatCopilot.WebApi.Models.Storage;
global using ChatCopilot.WebApi.Options;
global using ChatCopilot.WebApi.Plugins.Chat;
global using ChatCopilot.WebApi.Plugins.Utils;
global using ChatCopilot.WebApi.Services;
global using ChatCopilot.WebApi.Storage;
global using ChatCopilot.WebApi.Utilities;
global using Microsoft.ApplicationInsights;
global using Microsoft.ApplicationInsights.Channel;
global using Microsoft.ApplicationInsights.DataContracts;
global using Microsoft.ApplicationInsights.Extensibility;
global using Microsoft.ApplicationInsights.Extensibility.Implementation;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Hosting.Server;
global using Microsoft.AspNetCore.Hosting.Server.Features;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.Azure.Cosmos;
global using Microsoft.Extensions.Options;
global using Microsoft.Graph;
global using Microsoft.Identity.Web;
global using Microsoft.KernelMemory;
global using Microsoft.KernelMemory.Diagnostics;
global using Microsoft.KernelMemory.MemoryStorage.DevTools;
global using Microsoft.KernelMemory.Pipeline;
global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.ChatCompletion;
global using Microsoft.SemanticKernel.Connectors.OpenAI;
global using Microsoft.SemanticKernel.Plugins.Core;
global using Microsoft.SemanticKernel.Plugins.MsGraph;
global using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;
global using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors.Client;
global using Microsoft.SemanticKernel.Plugins.OpenApi;
global using SharpToken;
global using System.Collections.Concurrent;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Reflection;
global using System.Security.Claims;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using JsonSerializer = System.Text.Json.JsonSerializer;
