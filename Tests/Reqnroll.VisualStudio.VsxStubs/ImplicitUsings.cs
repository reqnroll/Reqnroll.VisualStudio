global using FluentAssertions;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.VisualStudio.Text;
global using Microsoft.VisualStudio.Text.Editor;
global using Microsoft.VisualStudio.Text.Formatting;
global using Microsoft.VisualStudio.Text.Projection;
global using Microsoft.VisualStudio.Text.Tagging;
global using Microsoft.VisualStudio.TextManager.Interop;
global using Microsoft.VisualStudio.Threading;
global using Microsoft.VisualStudio.Utilities;
global using NSubstitute;
global using Reqnroll.VisualStudio.Analytics;
global using Reqnroll.VisualStudio.Annotations;
global using Reqnroll.VisualStudio.Common;
global using Reqnroll.VisualStudio.Configuration;
global using Reqnroll.VisualStudio.Diagnostics;
global using Reqnroll.VisualStudio.Discovery;
global using Reqnroll.VisualStudio.Editor.Commands.Infrastructure;
global using Reqnroll.VisualStudio.Editor.Services;
global using Reqnroll.VisualStudio.Editor.Services.EditorConfig;
global using Reqnroll.VisualStudio.Monitoring;
global using Reqnroll.VisualStudio.ProjectSystem;
global using Reqnroll.VisualStudio.ProjectSystem.Actions;
global using Reqnroll.VisualStudio.ProjectSystem.Configuration;
global using Reqnroll.VisualStudio.ProjectSystem.Settings;
global using Reqnroll.VisualStudio.ReqnrollConnector.Models;
global using Reqnroll.VisualStudio.VsxStubs.ProjectSystem;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.IO;
global using System.IO.Abstractions;
global using System.IO.Abstractions.TestingHelpers;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Windows;
global using System.Windows.Media;
global using Xunit.Abstractions;
