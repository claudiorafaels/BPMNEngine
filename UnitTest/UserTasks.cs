﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPMNEngine;
using BPMNEngine.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using BPMNEngine.Interfaces.Tasks;
using System;

namespace UnitTest
{
    [TestClass]
    public class UserTasks
    {
        private static BusinessProcess _singleTaskProcess;
        private static BusinessProcess _multiTaskProcess;
        private const string _TEST_VARIABLE_NAME = "TestValue";
        private const string _TEST_VARIABLE_VALUE = "This is a test";
        private readonly static string[] _TEST_VARIABLE_VALUES = new string[]
        {
            "This is another test",
            "This is also a test",
            "Yup another test here"
        };
        private readonly static string[] _TEST_USER_IDS = new string[]
        {
            "User1","User2","User3"
        };

        [ClassInitialize]
        public static void Initialize(TestContext testContext) {
            _singleTaskProcess = new BusinessProcess(Utility.LoadResourceDocument("UserTasks/single_user_task.bpmn"));
            _multiTaskProcess = new BusinessProcess(Utility.LoadResourceDocument("UserTasks/multiple_user_tasks.bpmn"));
        }

        [ClassCleanup]
        public static void Cleanup() {
            _singleTaskProcess.Dispose();
            _multiTaskProcess.Dispose();
        }

        [TestMethod()]
        public void TestUserTaskAccess()
        {
            IProcessInstance instance = _singleTaskProcess.BeginProcess(new Dictionary<string, object>()
            {
                {_TEST_VARIABLE_NAME, _TEST_VARIABLE_VALUE}
            });
            Assert.IsNotNull(instance);

            Assert.IsTrue(instance.WaitForUserTask(TimeSpan.FromSeconds(5), "UserTask_15dj2au", out IUserTask task));
            Assert.IsNotNull(task);

            Assert.AreEqual(1, task.Variables.Keys.Count());
            Assert.AreEqual(_TEST_VARIABLE_VALUE, task.Variables[_TEST_VARIABLE_NAME]);
            task.UserID = _TEST_USER_IDS[0];
            task.Variables[_TEST_VARIABLE_NAME] = _TEST_VARIABLE_VALUES[0];
            task.MarkComplete();
            Assert.IsTrue(Utility.WaitForCompletion(instance));
            Dictionary<string, object> variables = instance.CurrentVariables;
            Assert.IsNotNull(variables);
            Assert.AreEqual(1, variables.Count);
            Assert.IsTrue(variables.ContainsKey(_TEST_VARIABLE_NAME));
            Assert.AreEqual(_TEST_VARIABLE_VALUES[0], variables[_TEST_VARIABLE_NAME]);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(instance.CurrentState.AsXMLDocument);
            Assert.IsNotNull(doc.SelectSingleNode(string.Format("/ProcessState/ProcessPath/sPathEntry[@elementID='UserTask_15dj2au'][@status='Succeeded'][@CompletedByID='{0}']", _TEST_USER_IDS[0])));
        }

        private static readonly string[] _TaskNames = new string[]
        {
            "UserTask_15dj2au",
            "UserTask_06twx0q",
            "UserTask_1qxmpii"
        };

        [TestMethod]
        public void TestUserTaskSeperation()
        {
            int idx = 0;
            IProcessInstance instance = _multiTaskProcess.BeginProcess(new Dictionary<string, object>()
            {
                {_TEST_VARIABLE_NAME, _TEST_VARIABLE_VALUE}
            });
            Assert.IsNotNull(instance);
            Thread.Sleep(6*1000);
            while (idx<3)
            {
                Assert.IsTrue(instance.WaitForUserTask(TimeSpan.FromSeconds(5), _TaskNames[idx], out IUserTask task));
                Assert.IsNotNull(task);
                Assert.IsNull(instance.GetUserTask(_TaskNames[idx+(idx==0 ? 1 : -1)]));
                Assert.AreEqual(1, task.Variables.Keys.Count());
                Assert.AreEqual((idx==0 ? _TEST_VARIABLE_VALUE : _TEST_VARIABLE_VALUES[idx-1]), task.Variables[_TEST_VARIABLE_NAME]);
                task.UserID = _TEST_USER_IDS[idx];
                task.Variables[_TEST_VARIABLE_NAME] = _TEST_VARIABLE_VALUES[idx];
                task.MarkComplete();
                idx++;
            }
            Assert.IsTrue(Utility.WaitForCompletion(instance));
            Dictionary<string, object> variables = instance.CurrentVariables;
            Assert.IsNotNull(variables);
            Assert.AreEqual(1, variables.Count);
            Assert.IsTrue(variables.ContainsKey(_TEST_VARIABLE_NAME));
            Assert.AreEqual(_TEST_VARIABLE_VALUES[2], variables[_TEST_VARIABLE_NAME]);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(instance.CurrentState.AsXMLDocument);
            Assert.IsNotNull(doc.SelectSingleNode(string.Format("/ProcessState/ProcessPath/sPathEntry[@elementID='{0}'][@status='Succeeded'][@CompletedByID='{1}']",new object[] { _TaskNames[0], _TEST_USER_IDS[0] })));
            Assert.IsNotNull(doc.SelectSingleNode(string.Format("/ProcessState/ProcessPath/sPathEntry[@elementID='{0}'][@status='Succeeded'][@CompletedByID='{1}']", new object[] { _TaskNames[1], _TEST_USER_IDS[1] })));
            Assert.IsNotNull(doc.SelectSingleNode(string.Format("/ProcessState/ProcessPath/sPathEntry[@elementID='{0}'][@status='Succeeded'][@CompletedByID='{1}']", new object[] { _TaskNames[2], _TEST_USER_IDS[2] })));
        }

        [TestMethod()]
        public void TestUserTaskElementPropertiesAccess()
        {
            IProcessInstance instance = _singleTaskProcess.BeginProcess(new Dictionary<string, object>()
            {
                {_TEST_VARIABLE_NAME, _TEST_VARIABLE_VALUE}
            });
            Assert.IsNotNull(instance);

            Assert.IsTrue(instance.WaitForUserTask(TimeSpan.FromSeconds(5), "UserTask_15dj2au", out IUserTask task));
            Assert.IsNotNull(task);
            
            Assert.AreEqual("UserTask_15dj2au", task.ID);
            Assert.AreEqual(task.ID, task["id"]);

            Assert.IsTrue(task.SubNodes.Any());
            Assert.AreEqual(2, task.SubNodes.Count());

            Assert.IsNull(task.ExtensionElement);
            Assert.IsNull(task.SubProcess);
            
            Assert.IsNotNull(task.Lane);
            Assert.AreEqual("Lane_0z6k1d4", task.Lane.ID);
            
            Assert.IsNotNull(task.Process);
            Assert.AreEqual("Process_1", task.Process.ID);

            task.MarkComplete();

            Assert.IsTrue(Utility.WaitForCompletion(instance));
        }
    }
}
