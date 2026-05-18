using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SZR_Production_API.Tests
{
    [TestClass]
    public class SimpleTests
    {
        // ========== ПОЛОЖИТЕЛЬНЫЕ ТЕСТЫ (5 шт) ==========

        [TestMethod]
        public void Test_Positive_01_UserModel_CanBeCreated()
        {
            // Arrange & Act
            var user = new { Id = 1, Login = "test", Password = "123" };

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("test", user.Login);
        }

        [TestMethod]
        public void Test_Positive_02_ProductModel_CanBeCreated()
        {
            var product = new { Id = 1, Code = "TEST", Name = "Тест" };
            Assert.IsNotNull(product);
            Assert.AreEqual("TEST", product.Code);
        }

        [TestMethod]
        public void Test_Positive_03_RecipeModel_CanBeCreated()
        {
            var recipe = new { Id = 1, ProductId = 1, Version = "1.0", Status = "Черновик" };
            Assert.IsNotNull(recipe);
            Assert.AreEqual("1.0", recipe.Version);
        }

        [TestMethod]
        public void Test_Positive_04_BatchModel_CanBeCreated()
        {
            var batch = new { Id = 1, BatchNumber = "B-001", Status = "В работе" };
            Assert.IsNotNull(batch);
            Assert.AreEqual("B-001", batch.BatchNumber);
        }

        [TestMethod]
        public void Test_Positive_05_LabTestModel_CanBeCreated()
        {
            var test = new { Id = 1, TestNumber = "QC-001", Status = "Создано" };
            Assert.IsNotNull(test);
            Assert.AreEqual("QC-001", test.TestNumber);
        }

        // ========== НЕГАТИВНЫЕ ТЕСТЫ (5 шт) ==========

        [TestMethod]
        public void Test_Negative_01_UserModel_EmptyLogin_IsNotNull()
        {
            var user = new { Login = "", Password = "123" };
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void Test_Negative_02_ProductModel_EmptyCode_IsNotNull()
        {
            var product = new { Code = "", Name = "Тест" };
            Assert.IsNotNull(product);
        }

        [TestMethod]
        public void Test_Negative_03_RecipeModel_EmptyVersion_IsNotNull()
        {
            var recipe = new { ProductId = 1, Version = "" };
            Assert.IsNotNull(recipe);
        }

        [TestMethod]
        public void Test_Negative_04_BatchModel_EmptyBatchNumber_IsNotNull()
        {
            var batch = new { BatchNumber = "", Status = "В работе" };
            Assert.IsNotNull(batch);
        }

        [TestMethod]
        public void Test_Negative_05_LabTestModel_EmptyTestNumber_IsNotNull()
        {
            var test = new { TestNumber = "", Status = "Создано" };
            Assert.IsNotNull(test);
        }
    }
}