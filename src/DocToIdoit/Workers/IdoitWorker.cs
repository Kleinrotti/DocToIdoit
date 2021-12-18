using IdoitSharp;
using IdoitSharp.CMDB.Category;
using IdoitSharp.CMDB.Object;
using IdoitSharp.Idoit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace DocToIdoit
{
    /// <summary>
    /// Provides functions to use the I-doit API.
    /// </summary>
    internal class IdoitWorker : IIdoitWorker
    {
        private readonly ILogger<IIdoitWorker> _logger;
        private readonly IConfiguration _configuration;
        private IdoitClient _idoitClient;

        public IdoitWorker(ILogger<IIdoitWorker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _idoitClient = new IdoitClient(_configuration["Idoit:Server"],
                _configuration["Idoit:ApiKey"], "de");
            _logger.LogDebug("IdoitWorker initialized");
        }

        public bool CreateObject(Product product)
        {
            try
            {
                if (ObjectExists(product))
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException, "Failed to check if idoit object already exists");
                throw;
            }
            int objectId;
            var objectRequest = new IdoitObjectInstance(_idoitClient);
            var modelRequest = new ModelRequest();
            var accountingRequest = new AccountingRequest();
            var modelInstance = new IdoitSvcInstance<ModelResponse>(_idoitClient);
            var accountingInstance = new IdoitSvcInstance<AccountingResponse>(_idoitClient);

            try
            {
                objectRequest.Type = product.Type;
                objectRequest.Value = product.IdoitPrefix + DateTime.Now.Ticks;
                objectRequest.Template = product.Template;
                objectId = objectRequest.Create();
                _logger.LogDebug($"Idoit object {objectRequest.Type} with value {objectRequest.Value} created");

                //Create the Category
                modelRequest.serial = product.SerialNumer;
                modelInstance.ObjectId = objectId;
                modelInstance.ObjectRequest = modelRequest;
                modelInstance.Update();
                _logger.LogDebug($"Added serial number to object {objectRequest.Value} with id {objectId}");

                accountingRequest.DeliveryNoteNumber = product.DeliveryNote;
                var date = product.OrderDate.ToString("yyyy-MM-dd");
                accountingRequest.DeliveryDate = date;
                accountingInstance.ObjectId = objectId;
                accountingInstance.ObjectRequest = accountingRequest;
                accountingInstance.Update();
                _logger.LogDebug($"Added accounting information to object {objectRequest.Value} with id {objectId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException, "Error during idoit object creation");
                throw;
            }
        }

        public void Dispose()
        {
            _idoitClient.Logout();
            _logger.LogDebug("IdoitWorker disposed");
        }

        /// <summary>
        /// Check if the serial number of the product already exists as an idoit object
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool ObjectExists(Product p)
        {
            _logger.LogDebug($"Checking if idoit object with S/N: {p.SerialNumer} already exists");
            try
            {
                var instance = new IdoitInstance(_idoitClient);
                var result = instance.Search(p.SerialNumer);
                if (result.Length > 0)
                {
                    _logger.LogWarning($"Idoit object with S/N: {p.SerialNumer} already exists. Skipping");
                    return true;
                }
                else
                {
                    _logger.LogDebug($"Idoit object with S/N: {p.SerialNumer} is unique");
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}