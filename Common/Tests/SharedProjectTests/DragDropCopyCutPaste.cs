﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using Microsoft.TC.TestHostAdapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using TestUtilities.SharedProject;
using TestUtilities.UI;
using Keyboard = TestUtilities.UI.Keyboard;
using Mouse = TestUtilities.UI.Mouse;

namespace Microsoft.VisualStudioTools.SharedProjectTests {
    [TestClass]
    public class DragDropCopyCutPaste : SharedProjectTest {
        [ClassInitialize]
        public static void DoDeployment(TestContext context) {
            AssertListener.Initialize();
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MultiPasteKeyboard() {
            MultiPaste(CopyByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MultiPasteMouse() {
            MultiPaste(CopyByMouse);
        }

        /// <summary>
        /// Cut item, paste into folder, paste into top-level, 2nd paste should prompt for overwrite
        /// </summary>
        private void MultiPaste(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("HelloWorld",
                    projectType,
                    ItemGroup(
                        Compile("server"),
                        Compile("server2"),
                        Folder("SubFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var server = solution.WaitForItem("HelloWorld", "server" + projectType.CodeExtension);
                    var server2 = solution.WaitForItem("HelloWorld", "server2" + projectType.CodeExtension);

                    mover(
                        solution.WaitForItem("HelloWorld", "SubFolder"),
                        solution.WaitForItem("HelloWorld", "server" + projectType.CodeExtension),
                        solution.WaitForItem("HelloWorld", "server2" + projectType.CodeExtension)
                    );

                    // paste once, multiple items should be pasted
                    Assert.IsNotNull(solution.WaitForItem("HelloWorld", "SubFolder", "server" + projectType.CodeExtension));
                    Assert.IsNotNull(solution.WaitForItem("HelloWorld", "SubFolder", "server2" + projectType.CodeExtension));

                    solution.SelectSolutionNode();

                    mover(
                        solution.WaitForItem("HelloWorld", "SubFolder"),
                        solution.WaitForItem("HelloWorld", "server" + projectType.CodeExtension),
                        solution.WaitForItem("HelloWorld", "server2" + projectType.CodeExtension)
                    );

                    // paste again, we should get the replace prompts...
                    var dialog = new OverwriteFileDialog(solution.App.WaitForDialog());
                    dialog.Cancel();

                    // https://pytools.codeplex.com/workitem/1154
                    // and we shouldn't get a second dialog after cancelling...
                    solution.App.WaitForDialogDismissed();
                }
            }
        }

        /// <summary>
        /// Cut item, paste into folder, paste into top-level, 2nd paste shouldn’t do anything
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutPastePasteItem() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Compile("CutPastePasteItem"),
                        Folder("PasteFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var project = solution.WaitForItem("DragDropCopyCutPaste");
                    var folder = solution.WaitForItem("DragDropCopyCutPaste", "PasteFolder");
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CutPastePasteItem" + projectType.CodeExtension);
                    AutomationWrapper.Select(file);

                    Keyboard.ControlX();

                    AutomationWrapper.Select(folder);
                    Keyboard.ControlV();
                    solution.AssertFileExists("DragDropCopyCutPaste", "PasteFolder", "CutPastePasteItem" + projectType.CodeExtension);

                    AutomationWrapper.Select(project);
                    Keyboard.ControlV();

                    System.Threading.Thread.Sleep(1000);

                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CutPastePasteItem" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Cut item, rename it, paste into top-level, check error message
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutRenamePaste() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CutRenamePaste"),
                        Compile("CutRenamePaste\\CutRenamePaste")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var project = solution.WaitForItem("DragDropCopyCutPaste");
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CutRenamePaste", "CutRenamePaste" + projectType.CodeExtension);

                    AutomationWrapper.Select(file);
                    Keyboard.ControlX();

                    AutomationWrapper.Select(file);
                    Keyboard.Type(Key.F2);
                    Keyboard.Type("CutRenamePasteNewName");
                    Keyboard.Type(Key.Enter);

                    System.Threading.Thread.Sleep(1000);
                    AutomationWrapper.Select(project);
                    Keyboard.ControlV();

                    VisualStudioApp.CheckMessageBox("The source URL 'CutRenamePaste" + projectType.CodeExtension + "' could not be found.");
                }
            }
        }

        /// <summary>
        /// Cut item, rename it, paste into top-level, check error message
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutDeletePaste() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CutDeletePaste"),
                        Compile("CutDeletePaste\\CutDeletePaste")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var project = solution.WaitForItem("DragDropCopyCutPaste");
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CutDeletePaste", "CutDeletePaste" + projectType.CodeExtension);

                    AutomationWrapper.Select(file);
                    Keyboard.ControlX();

                    File.Delete(Path.Combine(solution.Directory, @"DragDropCopyCutPaste\CutDeletePaste\CutDeletePaste" + projectType.CodeExtension));

                    AutomationWrapper.Select(project);
                    Keyboard.ControlV();

                    VisualStudioApp.CheckMessageBox("The item 'CutDeletePaste" + projectType.CodeExtension + "' does not exist in the project directory. It may have been moved, renamed or deleted.");

                    Assert.IsNotNull(solution.FindItem("DragDropCopyCutPaste", "CutDeletePaste", "CutDeletePaste" + projectType.CodeExtension));
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyFileToFolderTooLongKeyboard() {
            CopyFileToFolderTooLong(CopyByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyFileToFolderTooLongMouse() {
            CopyFileToFolderTooLong(CopyByMouse);
        }

        /// <summary>
        /// Adds a new folder which fits exactly w/ no space left in the path name
        /// </summary>
        private void CopyFileToFolderTooLong(MoveDelegate copier) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("LFN",
                    projectType,
                    ItemGroup(
                        Compile("server")
                    )
                );

                using (var solution = SolutionFile.Generate("LongFileNames", 29, testDef).ToVs()) {
                    // find server, send copy & paste, verify copy of file is there
                    var projectNode = solution.WaitForItem("LFN");
                    AutomationWrapper.Select(projectNode);

                    Keyboard.PressAndRelease(Key.F10, Key.LeftCtrl, Key.LeftShift);
                    Keyboard.PressAndRelease(Key.D);
                    Keyboard.PressAndRelease(Key.Right);
                    Keyboard.PressAndRelease(Key.D);
                    Keyboard.Type("01234567891");
                    Keyboard.PressAndRelease(Key.Enter);

                    var folderNode = solution.WaitForItem("LFN", "01234567891");
                    Assert.IsNotNull(folderNode);

                    var serverNode = solution.WaitForItem("LFN", "server" + projectType.CodeExtension);
                    AutomationWrapper.Select(serverNode);
                    Keyboard.ControlC();
                    Keyboard.ControlV();

                    var serverCopy = solution.WaitForItem("LFN", "server - Copy" + projectType.CodeExtension);
                    Assert.IsNotNull(serverCopy);

                    copier(folderNode, serverCopy);

                    VisualStudioApp.CheckMessageBox("The filename is too long.");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFileToFolderTooLongKeyboard() {
            CutFileToFolderTooLong(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFileToFolderTooLongMouse() {
            CutFileToFolderTooLong(MoveByMouse);
        }

        /// <summary>
        /// Adds a new folder which fits exactly w/ no space left in the path name
        /// </summary>
        private void CutFileToFolderTooLong(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("LFN",
                    projectType,
                    ItemGroup(
                        Compile("server")
                    )
                );

                using (var solution = SolutionFile.Generate("LongFileNames", 29, testDef).ToVs()) {
                    // find server, send copy & paste, verify copy of file is there
                    var projectNode = solution.WaitForItem("LFN");
                    AutomationWrapper.Select(projectNode);

                    Keyboard.PressAndRelease(Key.F10, Key.LeftCtrl, Key.LeftShift);
                    Keyboard.PressAndRelease(Key.D);
                    Keyboard.PressAndRelease(Key.Right);
                    Keyboard.PressAndRelease(Key.D);
                    Keyboard.Type("01234567891");
                    Keyboard.PressAndRelease(Key.Enter);

                    var folderNode = solution.WaitForItem("LFN", "01234567891");
                    Assert.IsNotNull(folderNode);

                    var serverNode = solution.FindItem("LFN", "server" + projectType.CodeExtension);
                    AutomationWrapper.Select(serverNode);
                    Keyboard.ControlC();
                    Keyboard.ControlV();

                    var serverCopy = solution.WaitForItem("LFN", "server - Copy" + projectType.CodeExtension);
                    Assert.IsNotNull(serverCopy);

                    mover(folderNode, serverCopy);

                    VisualStudioApp.CheckMessageBox("The filename is too long.");
                }
            }
        }

        /// <summary>
        /// Cut folder, rename it, paste into top-level, check error message
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutRenamePasteFolder() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CutRenamePaste"),
                        Folder("CutRenamePaste\\CutRenamePasteFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var project = solution.WaitForItem("DragDropCopyCutPaste");
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CutRenamePaste", "CutRenamePasteFolder");
                    AutomationWrapper.Select(file);
                    Keyboard.ControlX();

                    Keyboard.Type(Key.F2);
                    Keyboard.Type("CutRenamePasteFolderNewName");
                    Keyboard.Type(Key.Enter);
                    System.Threading.Thread.Sleep(1000);

                    AutomationWrapper.Select(project);
                    Keyboard.ControlV();

                    VisualStudioApp.CheckMessageBox("The source URL 'CutRenamePasteFolder' could not be found.");
                }
            }
        }

        /// <summary>
        /// Copy a file node, drag and drop a different file, paste the node, should succeed
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopiedBeforeDragPastedAfterDrop() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Compile("CopiedBeforeDragPastedAfterDrop"),
                        Compile("DragAndDroppedDuringCopy"),
                        Folder("DragDuringCopyDestination"),
                        Folder("PasteFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var project = solution.WaitForItem("DragDropCopyCutPaste");
                    Assert.AreNotEqual(null, project);
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CopiedBeforeDragPastedAfterDrop" + projectType.CodeExtension);
                    Assert.AreNotEqual(null, file);
                    var draggedFile = solution.WaitForItem("DragDropCopyCutPaste", "DragAndDroppedDuringCopy" + projectType.CodeExtension);
                    Assert.AreNotEqual(null, draggedFile);
                    var dragFolder = solution.WaitForItem("DragDropCopyCutPaste", "DragDuringCopyDestination");
                    Assert.AreNotEqual(null, dragFolder);

                    AutomationWrapper.Select(file);
                    Keyboard.ControlC();

                    MoveByMouse(
                        dragFolder,
                        draggedFile
                    );

                    var folder = solution.WaitForItem("DragDropCopyCutPaste", "PasteFolder");
                    AutomationWrapper.Select(folder);
                    Keyboard.ControlV();

                    solution.AssertFileExists("DragDropCopyCutPaste", "PasteFolder", "CopiedBeforeDragPastedAfterDrop" + projectType.CodeExtension);
                    solution.AssertFileExists("DragDropCopyCutPaste", "CopiedBeforeDragPastedAfterDrop" + projectType.CodeExtension);
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragToAnotherProjectKeyboard() {
            DragToAnotherProject(CopyByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragToAnotherProjectMouse() {
            DragToAnotherProject(DragAndDrop);
        }

        /// <summary>
        /// Copy from CSharp into our project
        /// </summary>
        private void DragToAnotherProject(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Folder("!Source"),
                            Compile("!Source\\DraggedToOtherProject")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Folder("DraggedToOtherProject")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    mover(
                        solution.WaitForItem("ConsoleApplication1"),
                        solution.WaitForItem("DragDropCopyCutPaste", "!Source", "DraggedToOtherProject" + projectType.CodeExtension)
                    );

                    solution.AssertFileExists("ConsoleApplication1", "DraggedToOtherProject" + projectType.CodeExtension);
                    solution.AssertFileExists("DragDropCopyCutPaste", "!Source", "DraggedToOtherProject" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Cut folder, paste onto itself, should report an error that the destination is the same as the source
        ///     Cannot move 'X'. The destination folder is the same as the source folder.
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFolderPasteOnSelf() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CutFolderPasteOnSelf")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    MoveByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste", "CutFolderPasteOnSelf"),
                        solution.WaitForItem("DragDropCopyCutPaste", "CutFolderPasteOnSelf")
                    );

                    VisualStudioApp.CheckMessageBox("Cannot move 'CutFolderPasteOnSelf'. The destination folder is the same as the source folder.");

                    solution.AssertFolderExists("DragDropCopyCutPaste", "CutFolderPasteOnSelf");
                    solution.AssertFolderDoesntExist("DragDropCopyCutPaste", "CutFolderPasteOnSelf - Copy");
                }
            }
        }

        /// <summary>
        /// Drag and drop a folder onto itself, nothing should happen
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragFolderOntoSelf() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("DragFolderOntoSelf"),
                        Compile("DragFolderOntoSelf\\File")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var draggedFolder = solution.WaitForItem("DragDropCopyCutPaste", "DragFolderOntoSelf");
                    AutomationWrapper.Select(draggedFolder);

                    var point = draggedFolder.GetClickablePoint();
                    Mouse.MoveTo(point);
                    Mouse.Down(MouseButton.Left);
                    Mouse.MoveTo(new Point(point.X + 1, point.Y + 1));

                    Mouse.Up(MouseButton.Left);

                    solution.AssertFolderExists("DragDropCopyCutPaste", "DragFolderOntoSelf");
                    solution.AssertFileExists("DragDropCopyCutPaste", "DragFolderOntoSelf", "File" + projectType.CodeExtension);
                    solution.AssertFolderDoesntExist("DragDropCopyCutPaste", "DragFolderOntoSelf - Copy");
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "DragFolderOntoSelf", "File - Copy" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Drag and drop a folder onto itself, nothing should happen
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragFolderOntoChild() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("ParentFolder"),
                        Folder("ParentFolder\\ChildFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    MoveByMouse(
                        solution.WaitForItem("DragDropCopyCutPaste", "ParentFolder", "ChildFolder"),
                        solution.WaitForItem("DragDropCopyCutPaste", "ParentFolder")
                    );

                    VisualStudioApp.CheckMessageBox("Cannot move 'ParentFolder'. The destination folder is a subfolder of the source folder.");
                    solution.App.WaitForDialogDismissed();

                    var draggedFolder = solution.FindItem("DragDropCopyCutPaste", "ParentFolder");
                    Assert.IsNotNull(draggedFolder);
                    var childFolder = solution.FindItem("DragDropCopyCutPaste", "ParentFolder", "ChildFolder");
                    Assert.IsNotNull(childFolder);
                    var parentInChildFolder = solution.FindItem("DragDropCopyCutPaste", "ParentFolder", "ChildFolder", "ParentFolder");
                    Assert.IsNull(parentInChildFolder);
                }
            }
        }

        /// <summary>
        /// Move a file to a location where a file with the name now already exists.  We should get an overwrite
        /// dialog, and after answering yes to overwrite the file should be moved.
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFileReplace() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("MoveDupFilename"),
                        Folder("MoveDupFilename\\Fob"),
                        Compile("MoveDupFilename\\Fob\\server"),
                        Compile("MoveDupFilename\\server")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    MoveByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste", "MoveDupFilename"),
                        solution.WaitForItem("DragDropCopyCutPaste", "MoveDupFilename", "Fob", "server" + projectType.CodeExtension)
                    );

                    var dialog = new OverwriteFileDialog(solution.App.WaitForDialog());
                    dialog.Yes();

                    solution.AssertFileExists("DragDropCopyCutPaste", "MoveDupFilename", "server" + projectType.CodeExtension);
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "MoveDupFilename", "Fob", "server" + projectType.CodeExtension);
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFolderAndFile() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CutFolderAndFile"),
                        Folder("CutFolderAndFile\\CutFolder"),
                        Compile("CutFolderAndFile\\CutFolder\\CutFolderAndFile")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var folder = solution.WaitForItem("DragDropCopyCutPaste", "CutFolderAndFile", "CutFolder");
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CutFolderAndFile", "CutFolder", "CutFolderAndFile" + projectType.CodeExtension);
                    var dest = solution.WaitForItem("DragDropCopyCutPaste");

                    AutomationWrapper.Select(folder);
                    AutomationWrapper.AddToSelection(file);

                    Keyboard.ControlX();
                    AutomationWrapper.Select(dest);
                    Keyboard.ControlV();

                    solution.AssertFileExists("DragDropCopyCutPaste", "CutFolder", "CutFolderAndFile" + projectType.CodeExtension);
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CutFolderAndFile", "CutFolder");
                }
            }
        }

        /// <summary>
        /// Drag and drop a folder onto itself, nothing should happen
        ///     Cannot move 'CutFilePasteSameLocation'. The destination folder is the same as the source folder.
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CutFilePasteSameLocation() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Compile("CutFilePasteSameLocation")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    MoveByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste"),
                        solution.WaitForItem("DragDropCopyCutPaste", "CutFilePasteSameLocation" + projectType.CodeExtension)
                    );

                    VisualStudioApp.CheckMessageBox("Cannot move 'CutFilePasteSameLocation" + projectType.CodeExtension + "'. The destination folder is the same as the source folder.");

                    solution.AssertFileExists("DragDropCopyCutPaste", "CutFilePasteSameLocation" + projectType.CodeExtension);
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CutFilePasteSameLocation - Copy" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Drag and drop a folder onto itself, nothing should happen
        ///     Cannot move 'DragFolderAndFileToSameFolder'. The destination folder is the same as the source folder.
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragFolderAndFileOntoSelf() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("DragFolderAndFileOntoSelf"),
                        Compile("DragFolderAndFileOntoSelf\\File")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var folder = solution.WaitForItem("DragDropCopyCutPaste", "DragFolderAndFileOntoSelf");
                    DragAndDrop(
                        folder,
                        folder,
                        solution.WaitForItem("DragDropCopyCutPaste", "DragFolderAndFileOntoSelf", "File" + projectType.CodeExtension)
                    );

                    VisualStudioApp.CheckMessageBox("Cannot move 'DragFolderAndFileOntoSelf'. The destination folder is the same as the source folder.");
                }
            }
        }

        /// <summary>
        /// Add folder from another project, folder contains items on disk which are not in the project, only items in the project should be added.
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyFolderFromAnotherHierarchy() {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Folder("!Source"),
                            Compile("!Source\\DraggedToOtherProject")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Folder("CopiedFolderWithItemsNotInProject"),
                            Compile("CopiedFolderWithItemsNotInProject\\Class"),
                            Content("CopiedFolderWithItemsNotInProject\\Text.txt", "", isExcluded:true)
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    CopyByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste"),
                        solution.WaitForItem("ConsoleApplication1", "CopiedFolderWithItemsNotInProject")
                    );

                    solution.WaitForItem("DragDropCopyCutPaste", "CopiedFolderWithItemsNotInProject", "Class.cs");

                    solution.AssertFolderExists("DragDropCopyCutPaste", "CopiedFolderWithItemsNotInProject");
                    solution.AssertFileExists("DragDropCopyCutPaste", "CopiedFolderWithItemsNotInProject", "Class.cs");
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CopiedFolderWithItemsNotInProject", "Text.txt");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyDeletePaste() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CopyDeletePaste"),
                        Compile("CopyDeletePaste\\CopyDeletePaste")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    var file = solution.WaitForItem("DragDropCopyCutPaste", "CopyDeletePaste", "CopyDeletePaste" + projectType.CodeExtension);
                    var project = solution.WaitForItem("DragDropCopyCutPaste");

                    AutomationWrapper.Select(file);
                    Keyboard.ControlC();

                    AutomationWrapper.Select(file);
                    Keyboard.Type(Key.Delete);
                    solution.App.WaitForDialog();

                    Keyboard.Type("\r");

                    AutomationWrapper.Select(project);
                    Keyboard.ControlV();

                    VisualStudioApp.CheckMessageBox("The source URL 'CopyDeletePaste" + projectType.CodeExtension + "' could not be found.");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CrossHierarchyFileDragAndDropKeyboard() {
            CrossHierarchyFileDragAndDrop(CopyByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CrossHierarchyFileDragAndDropMouse() {
            CrossHierarchyFileDragAndDrop(DragAndDrop);
        }

        /// <summary>
        /// Copy from C# into our project
        /// </summary>
        private void CrossHierarchyFileDragAndDrop(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Folder("DropFolder")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Compile("CrossHierarchyFileDragAndDrop")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    mover(
                        solution.WaitForItem("DragDropCopyCutPaste", "DropFolder"),
                        solution.WaitForItem("ConsoleApplication1", "CrossHierarchyFileDragAndDrop.cs")
                    );

                    solution.AssertFileExists("DragDropCopyCutPaste", "DropFolder", "CrossHierarchyFileDragAndDrop.cs");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveDuplicateFolderNameKeyboard() {
            MoveDuplicateFolderName(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveDuplicateFolderNameMouse() {
            MoveDuplicateFolderName(MoveByMouse);
        }

        /// <summary>
        /// Drag file from another hierarchy into folder in our hierarchy, item should be added
        ///     Cannot move the folder 'DuplicateFolderName'. A folder with that name already exists in the destination directory.
        /// </summary>
        private void MoveDuplicateFolderName(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("DuplicateFolderName"),
                        Folder("DuplicateFolderNameTarget"),
                        Folder("DuplicateFolderNameTarget\\DuplicateFolderName")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.WaitForItem("DragDropCopyCutPaste", "DuplicateFolderNameTarget"),
                        solution.WaitForItem("DragDropCopyCutPaste", "DuplicateFolderName")
                    );

                    VisualStudioApp.CheckMessageBox("Cannot move the folder 'DuplicateFolderName'. A folder with that name already exists in the destination directory.");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyDuplicateFolderNameKeyboard() {
            CopyDuplicateFolderName(CopyByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyDuplicateFolderNameMouse() {
            CopyDuplicateFolderName(CopyByMouse);
        }

        /// <summary>
        /// Copy folder to a destination where the folder already exists.  Say don't copy, nothing should be copied.
        /// </summary>
        private void CopyDuplicateFolderName(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CopyDuplicateFolderName"),
                        Compile("CopyDuplicateFolderName\\server"),
                        Folder("CopyDuplicateFolderNameTarget"),
                        Folder("CopyDuplicateFolderNameTarget\\CopyDuplicateFolderName")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    mover(
                        solution.WaitForItem("DragDropCopyCutPaste", "CopyDuplicateFolderNameTarget"),
                        solution.WaitForItem("DragDropCopyCutPaste", "CopyDuplicateFolderName")
                    );

                    var dialog = new OverwriteFileDialog(solution.App.WaitForDialog());
                    Assert.IsTrue(dialog.Text.Contains("This folder already contains a folder called 'CopyDuplicateFolderName'"), "wrong text in overwrite dialog");
                    dialog.No();

                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CopyDuplicateFolderNameTarget", "CopyDuplicateFolderName", "server" + projectType.CodeExtension);
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveCrossHierarchyKeyboard() {
            MoveCrossHierarchy(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveCrossHierarchyMouse() {
            MoveCrossHierarchy(MoveByMouse);
        }

        /// <summary>
        /// Cut item from one project, paste into another project, item should be removed from original project
        /// </summary>
        private void MoveCrossHierarchy(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Folder("!Source"),
                            Compile("!Source\\DraggedToOtherProject")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Compile("CrossHierarchyCut")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    mover(
                        solution.WaitForItem("DragDropCopyCutPaste"),
                        solution.WaitForItem("ConsoleApplication1", "CrossHierarchyCut.cs")
                    );

                    solution.AssertFileExists("DragDropCopyCutPaste", "CrossHierarchyCut.cs");
                    solution.AssertFileDoesntExist("ConsoleApplication1", "CrossHierarchyCut.cs");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveReverseCrossHierarchyKeyboard() {
            MoveReverseCrossHierarchy(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveReverseCrossHierarchyMouse() {
            MoveReverseCrossHierarchy(MoveByMouse);
        }

        /// <summary>
        /// Cut an item from our project, paste into another project, item should be removed from our project
        /// </summary>
        private void MoveReverseCrossHierarchy(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Compile("CrossHierarchyCut")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    mover(
                        solution.WaitForItem("ConsoleApplication1"),
                        solution.WaitForItem("DragDropCopyCutPaste", "CrossHierarchyCut" + projectType.CodeExtension)
                    );

                    solution.AssertFileExists("ConsoleApplication1", "CrossHierarchyCut" + projectType.CodeExtension);
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "CrossHierarchyCut" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Drag item from our project to other project, copy
        /// Drag item from other project to our project, still copy back
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveDoubleCrossHierarchy() {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType,
                        ItemGroup(
                            Folder("!Source"),
                            Compile("!Source\\DoubleCrossHierarchy")
                        )
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Compile("DoubleCrossHierarchy")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    DragAndDrop(
                        solution.WaitForItem("ConsoleApplication1"),
                        solution.WaitForItem("DragDropCopyCutPaste", "!Source", "DoubleCrossHierarchy" + projectType.CodeExtension)
                    );

                    solution.AssertFileExists("ConsoleApplication1", "DoubleCrossHierarchy" + projectType.CodeExtension);
                    solution.AssertFileExists("DragDropCopyCutPaste", "!Source", "DoubleCrossHierarchy" + projectType.CodeExtension);

                    DragAndDrop(
                        solution.FindItem("DragDropCopyCutPaste"),
                        solution.FindItem("ConsoleApplication1", "DoubleCrossHierarchy.cs")
                    );

                    solution.AssertFileExists("DragDropCopyCutPaste", "DoubleCrossHierarchy.cs");
                    solution.AssertFileExists("ConsoleApplication1", "DoubleCrossHierarchy.cs");
                }
            }
        }

        /// <summary>
        /// Drag item from another project, drag same item again, prompt to overwrite, say yes, only one item should be in the hierarchy
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void DragTwiceAndOverwrite() {
            foreach (var projectType in ProjectTypes) {
                var projects = new[] {
                    new ProjectDefinition(
                        "DragDropCopyCutPaste",
                        projectType
                    ),
                    new ProjectDefinition(
                        "ConsoleApplication1",
                        ProjectType.CSharp,
                        ItemGroup(
                            Folder("DraggedToOtherProject"),
                            Compile("DragTwiceAndOverwrite")
                        )
                    )
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    for (int i = 0; i < 2; i++) {
                        DragAndDrop(
                            solution.WaitForItem("DragDropCopyCutPaste"),
                            solution.WaitForItem("ConsoleApplication1", "DragTwiceAndOverwrite.cs")
                        );
                    }

                    var dialog = new OverwriteFileDialog(solution.App.WaitForDialog());
                    Assert.IsTrue(dialog.Text.Contains("A file with the name 'DragTwiceAndOverwrite.cs' already exists."), "wrong text");
                    dialog.Yes();

                    solution.AssertFileExists("DragDropCopyCutPaste", "DragTwiceAndOverwrite.cs");
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "DragTwiceAndOverwrite - Copy.cs");
                }
            }
        }

        /// <summary>
        /// Drag item from another project, drag same item again, prompt to overwrite, say yes, only one item should be in the hierarchy
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyFolderMissingItem() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("CopyFolderMissingItem"),
                        Compile("CopyFolderMissingItem\\missing", isMissing: true),
                        Folder("PasteFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    CopyByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste", "PasteFolder"),
                        solution.WaitForItem("DragDropCopyCutPaste", "CopyFolderMissingItem")
                    );

                    // make sure no dialogs pop up
                    VisualStudioApp.CheckMessageBox("The item 'missing" + projectType.CodeExtension + "' does not exist in the project directory. It may have been moved, renamed or deleted.");

                    solution.AssertFolderExists("DragDropCopyCutPaste", "CopyFolderMissingItem");
                    solution.AssertFolderDoesntExist("DragDropCopyCutPaste", "PasteFolder", "CopyFolderMissingItem");
                    solution.AssertFileDoesntExist("DragDropCopyCutPaste", "PasteFolder", "missing" + projectType.CodeExtension);
                }
            }
        }

        /// <summary>
        /// Copy missing file
        /// 
        /// https://pytools.codeplex.com/workitem/1141
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CopyPasteMissingFile() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Compile("MissingFile", isMissing: true),
                        Folder("PasteFolder")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    CopyByKeyboard(
                        solution.WaitForItem("DragDropCopyCutPaste", "PasteFolder"),
                        solution.WaitForItem("DragDropCopyCutPaste", "MissingFile" + projectType.CodeExtension)
                    );

                    VisualStudioApp.CheckMessageBox("The item 'MissingFile" + projectType.CodeExtension + "' does not exist in the project directory. It may have been moved, renamed or deleted.");
                }
            }
        }

        /// <summary>
        /// Drag folder to a location where a file with the same name already exists.
        /// 
        /// https://nodejstools.codeplex.com/workitem/241
        /// </summary>
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveFolderExistingFile() {
            foreach (var projectType in ProjectTypes) {
                var testDef = new ProjectDefinition("DragDropCopyCutPaste",
                    projectType,
                    ItemGroup(
                        Folder("PasteFolder"),
                        Content("PasteFolder\\FolderCollision", ""),
                        Folder("FolderCollision")
                    )
                );

                using (var solution = testDef.Generate().ToVs()) {
                    MoveByKeyboard(
                        solution.FindItem("DragDropCopyCutPaste", "PasteFolder"),
                        solution.FindItem("DragDropCopyCutPaste", "FolderCollision")
                    );

                    VisualStudioApp.CheckMessageBox("Unable to add 'FolderCollision'. A file with that name already exists.");
                }
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveProjectToSolutionFolderKeyboard() {
            MoveProjectToSolutionFolder(MoveByKeyboard);
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void MoveProjectToSolutionFolderMouse() {
            MoveProjectToSolutionFolder(MoveByMouse);
        }

        /// <summary>
        /// Cut an item from our project, paste into another project, item should be removed from our project
        /// </summary>
        private void MoveProjectToSolutionFolder(MoveDelegate mover) {
            foreach (var projectType in ProjectTypes) {
                var projects = new ISolutionElement[] {
                    new ProjectDefinition("DragDropCopyCutPaste", projectType),
                    SolutionFolder("SolFolder")
                };

                using (var solution = SolutionFile.Generate("DragDropCopyCutPaste", projects).ToVs()) {
                    mover(
                        solution.WaitForItem("SolFolder"),
                        solution.WaitForItem("DragDropCopyCutPaste")
                    );

                    Assert.IsNotNull(solution.WaitForItem("SolFolder", "DragDropCopyCutPaste"));
                }
            }
        }

        private delegate void MoveDelegate(AutomationElement destination, params AutomationElement[] source);

        /// <summary>
        /// Moves one or more items in solution explorer to the destination using the mouse.
        /// </summary>
        private static void MoveByMouse(AutomationElement destination, params AutomationElement[] source) {
            SelectItemsForDragAndDrop(source);

            try {
                try {
                    Keyboard.Press(Key.LeftShift);
                    Mouse.MoveTo(destination.GetClickablePoint());
                } finally {
                    Mouse.Up(MouseButton.Left);
                }
            } finally {
                Keyboard.Release(Key.LeftShift);
            }
        }

        /// <summary>
        /// Moves or copies (taking the default behavior) one or more items in solution explorer to 
        /// the destination using the mouse.
        /// </summary>
        private static void DragAndDrop(AutomationElement destination, params AutomationElement[] source) {
            SelectItemsForDragAndDrop(source);

            try {
                Mouse.MoveTo(destination.GetClickablePoint());
            } finally {
                Mouse.Up(MouseButton.Left);
            }
        }

        /// <summary>
        /// Moves one or more items in solution explorer to the destination using the mouse.
        /// </summary>
        private static void CopyByMouse(AutomationElement destination, params AutomationElement[] source) {
            SelectItemsForDragAndDrop(source);

            try {
                try {
                    Keyboard.Press(Key.LeftCtrl);
                    Mouse.MoveTo(destination.GetClickablePoint());
                } finally {
                    Mouse.Up(MouseButton.Left);
                }
            } finally {
                Keyboard.Release(Key.LeftCtrl);
            }
        }

        /// <summary>
        /// Selects the provided items with the mouse preparing for a drag and drop
        /// </summary>
        /// <param name="source"></param>
        private static void SelectItemsForDragAndDrop(AutomationElement[] source) {
            AutomationWrapper.Select(source.First());
            for (int i = 1; i < source.Length; i++) {
                AutomationWrapper.AddToSelection(source[i]);
            }

            Mouse.MoveTo(source.Last().GetClickablePoint());
            Mouse.Down(MouseButton.Left);
        }

        /// <summary>
        /// Moves one or more items in solution explorer using the keyboard to cut and paste.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        private static void MoveByKeyboard(AutomationElement destination, params AutomationElement[] source) {
            AutomationWrapper.Select(source.First());
            for (int i = 1; i < source.Length; i++) {
                AutomationWrapper.AddToSelection(source[i]);
            }

            Keyboard.ControlX();

            AutomationWrapper.Select(destination);
            Keyboard.ControlV();
        }

        /// <summary>
        /// Moves one or more items in solution explorer using the keyboard to cut and paste.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        private static void CopyByKeyboard(AutomationElement destination, params AutomationElement[] source) {
            AutomationWrapper.Select(source.First());
            for (int i = 1; i < source.Length; i++) {
                AutomationWrapper.AddToSelection(source[i]);
            }

            Keyboard.ControlC();

            AutomationWrapper.Select(destination);
            Keyboard.ControlV();
        }
    }
}