global using EnvDTE;
//global using Microsoft.CodeAnalysis;//Causes many conflicts with EnvDTE.Project
global using Microsoft.CodeAnalysis.Text;
global using Microsoft.VisualStudio;
global using Microsoft.VisualStudio.ComponentModelHost;
global using Microsoft.VisualStudio.Editor;
//global using Microsoft.VisualStudio.OLE.Interop; //Causes many conflicts with System.IServiceProvider 
global using Microsoft.VisualStudio.Shell;
global using Microsoft.VisualStudio.Shell.Events;
global using Microsoft.VisualStudio.Shell.Interop;
global using Microsoft.VisualStudio.Shell.ServiceBroker;
global using Microsoft.VisualStudio.Text;
global using Microsoft.VisualStudio.Text.Editor;
global using Microsoft.VisualStudio.TextManager.Interop;
global using Microsoft.VisualStudio.Threading;
global using Microsoft.VisualStudio.Utilities;
global using NuGet.VisualStudio.Contracts;
global using Reqnroll.VisualStudio.Analytics;
global using Reqnroll.VisualStudio.Annotations;
global using Reqnroll.VisualStudio.Common;
global using Reqnroll.VisualStudio.Diagnostics;
global using Reqnroll.VisualStudio.Discovery;
global using Reqnroll.VisualStudio.Editor.Commands;
global using Reqnroll.VisualStudio.Interop;
global using Reqnroll.VisualStudio.Monitoring;
global using Reqnroll.VisualStudio.Notifications;
global using Reqnroll.VisualStudio.ProjectSystem;
global using Reqnroll.VisualStudio.ProjectSystem.Actions;
global using Reqnroll.VisualStudio.ProjectSystem.Settings;
global using Reqnroll.VisualStudio.UI;
global using Reqnroll.VisualStudio.UI.ViewModels;
global using Reqnroll.VisualStudio.VsEvents;
global using System.Collections.Concurrent;
global using System.Collections.Immutable;
global using System.ComponentModel.Composition;
global using System.Diagnostics;
global using System.IO;
global using System.IO.Abstractions;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Windows;
global using System.Windows.Media;
global using System.Windows.Threading;
