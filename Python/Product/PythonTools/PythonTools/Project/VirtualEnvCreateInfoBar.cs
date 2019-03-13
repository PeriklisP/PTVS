﻿// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.PythonTools.Environments;
using Microsoft.PythonTools.Infrastructure;
using Microsoft.PythonTools.Interpreter;
using Microsoft.PythonTools.Logging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.PythonTools.Project {
    internal sealed class VirtualEnvCreateInfoBar : PythonProjectInfoBar {
        public VirtualEnvCreateInfoBar(IServiceProvider site, PythonProjectNode projectNode, IPythonWorkspaceContext workspace)
            : base(site, projectNode, workspace) {
        }

        public override async Task CheckAsync() {
            if (IsCreated) {
                return;
            }

            var projectOrWorkspaceName = Project?.Caption ?? Workspace?.WorkspaceName ?? string.Empty;

            if (!Site.GetPythonToolsService().GeneralOptions.PromptForEnvCreate) {
                return;
            }

            if (IsSuppressed(PythonConstants.SuppressEnvironmentCreationPrompt)) {
                return;
            }

            var txtPath = Project?.GetRequirementsTxtPath() ?? Workspace?.GetRequirementsTxtPath();
            if (!File.Exists(txtPath)) {
                return;
            }

            if (Project?.IsActiveInterpreterGlobalDefault == false || Workspace?.IsCurrentFactoryDefault == false) {
                return;
            }

            Action createVirtualEnv = () => {
                Logger?.LogEvent(
                    PythonLogEvent.VirtualEnvCreateInfoBar,
                    new VirtualEnvCreateInfoBarInfo() {
                        Action = VirtualEnvCreateInfoBarActions.Create,
                    }
                );
                AddEnvironmentDialog.ShowAddVirtualEnvironmentDialogAsync(
                    Site,
                    Project,
                    Workspace,
                    null,
                    null,
                    txtPath
                ).HandleAllExceptions(Site, typeof(VirtualEnvCreateInfoBar)).DoNotWait();
                Close();
            };

            Action projectIgnore = () => {
                Logger?.LogEvent(
                    PythonLogEvent.VirtualEnvCreateInfoBar,
                    new VirtualEnvCreateInfoBarInfo() {
                        Action = VirtualEnvCreateInfoBarActions.Ignore,
                    }
                );
                SuppressAsync(PythonConstants.SuppressEnvironmentCreationPrompt)
                    .HandleAllExceptions(Site, typeof(VirtualEnvCreateInfoBar))
                    .DoNotWait();
                Close();
            };

            var messages = new List<IVsInfoBarTextSpan>();
            var actions = new List<InfoBarActionItem>();

            messages.Add(new InfoBarTextSpan(
                Strings.RequirementsTxtCreateVirtualEnvInfoBarMessage.FormatUI(
                    PathUtils.GetFileOrDirectoryName(txtPath),
                    projectOrWorkspaceName
            )));
            actions.Add(new InfoBarHyperlink(Strings.RequirementsTxtInfoBarCreateVirtualEnvAction, createVirtualEnv));
            actions.Add(new InfoBarHyperlink(Strings.RequirementsTxtInfoBarProjectIgnoreAction, projectIgnore));

            Logger?.LogEvent(
                PythonLogEvent.VirtualEnvCreateInfoBar,
                new VirtualEnvCreateInfoBarInfo() {
                    Action = VirtualEnvCreateInfoBarActions.Prompt,
                }
            );

            Create(new InfoBarModel(messages, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: true));
        }
    }
}
