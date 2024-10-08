global using Microsoft;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Text;
global using Microsoft.VisualStudio;
global using Microsoft.VisualStudio.ApplicationInsights;
global using Microsoft.VisualStudio.ApplicationInsights.Channel;
global using Microsoft.VisualStudio.ApplicationInsights.DataContracts;
global using Microsoft.VisualStudio.ApplicationInsights.Extensibility;
global using Microsoft.VisualStudio.Editor;
global using Microsoft.VisualStudio.Language.Intellisense;
global using Microsoft.VisualStudio.PlatformUI;
//global using Microsoft.VisualStudio.OLE.Interop; //Causes many conflicts with System.IServiceProvider 
global using Microsoft.VisualStudio.Shell;
global using Microsoft.VisualStudio.Shell.Interop;
global using Microsoft.VisualStudio.Text;
global using Microsoft.VisualStudio.Text.Editor;
global using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
global using Microsoft.VisualStudio.Text.Tagging;
global using Microsoft.VisualStudio.TextManager.Interop;
global using Microsoft.VisualStudio.Threading;
global using Microsoft.VisualStudio.Utilities;
global using Reqnroll.VisualStudio.Analytics;
global using Reqnroll.VisualStudio.Annotations;
global using Reqnroll.VisualStudio.Common;
global using Reqnroll.VisualStudio.Configuration;
global using Reqnroll.VisualStudio.Connectors;
global using Reqnroll.VisualStudio.Diagnostics;
global using Reqnroll.VisualStudio.Discovery;
global using Reqnroll.VisualStudio.Editor.Commands;
global using Reqnroll.VisualStudio.Editor.Commands.Infrastructure;
global using Reqnroll.VisualStudio.Editor.Completions.Infrastructure;
global using Reqnroll.VisualStudio.Editor.Services;
global using Reqnroll.VisualStudio.Editor.Services.EditorConfig;
global using Reqnroll.VisualStudio.Editor.Services.Formatting;
global using Reqnroll.VisualStudio.Editor.Services.Parser;
global using Reqnroll.VisualStudio.Editor.Services.StepDefinitions;
global using Reqnroll.VisualStudio.Monitoring;
global using Reqnroll.VisualStudio.Notifications;
global using Reqnroll.VisualStudio.ProjectSystem;
global using Reqnroll.VisualStudio.ProjectSystem.Actions;
global using Reqnroll.VisualStudio.ProjectSystem.Configuration;
global using Reqnroll.VisualStudio.ProjectSystem.Settings;
global using Reqnroll.VisualStudio.Snippets;
global using Reqnroll.VisualStudio.Snippets.Fallback;
global using Reqnroll.VisualStudio.ReqnrollConnector;
global using Reqnroll.VisualStudio.UI.ViewModels;
global using Reqnroll.VisualStudio.UI.ViewModels.WizardDialogs;
global using System.Collections;
global using System.Collections.Immutable;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.ComponentModel.Composition;
global using System.IO;
global using System.IO.Abstractions;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Threading.Channels;
global using System.Windows.Controls;
global using System.Windows.Controls.Primitives;
global using System.Windows.Input;
global using System.Windows.Threading;
