using System;
using System.Net;
using System.Web.Http;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/statuses")]
    public class StatusesController : ApiController
    {
        // GET: api/statuses/all
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetAllStatuses()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        product = GetProductStatusList(),
                        order = GetOrderStatusList(),
                        batch = GetBatchStatusList(),
                        step = GetStepStatusList(),
                        recipe = GetRecipeStatusList(),
                        techCard = GetTechCardStatusList(),
                        labBatch = GetLabBatchStatusList(),
                        labTest = GetLabTestStatusList(),
                        deviation = GetDeviationSeverityList(),
                        extruderProgram = GetExtruderProgramStatusList()
                    },
                    message = "Справочник статусов получен",
                    pagination = (object)null
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов: " + ex.Message);
            }
        }

        // GET: api/statuses/product
        [HttpGet]
        [Route("product")]
        public IHttpActionResult GetProductStatuses()
        {
            try
            {
                return OkResponse(GetProductStatusList(), "Статусы продукции получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов продукции: " + ex.Message);
            }
        }

        // GET: api/statuses/order
        [HttpGet]
        [Route("order")]
        public IHttpActionResult GetOrderStatuses()
        {
            try
            {
                return OkResponse(GetOrderStatusList(), "Статусы заказов получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов заказа: " + ex.Message);
            }
        }

        // GET: api/statuses/batch
        [HttpGet]
        [Route("batch")]
        public IHttpActionResult GetBatchStatuses()
        {
            try
            {
                return OkResponse(GetBatchStatusList(), "Статусы производственных партий получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов партии: " + ex.Message);
            }
        }

        // GET: api/statuses/step
        [HttpGet]
        [Route("step")]
        public IHttpActionResult GetStepStatuses()
        {
            try
            {
                return OkResponse(GetStepStatusList(), "Статусы шагов получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов шага: " + ex.Message);
            }
        }

        // GET: api/statuses/recipe
        [HttpGet]
        [Route("recipe")]
        public IHttpActionResult GetRecipeStatuses()
        {
            try
            {
                return OkResponse(GetRecipeStatusList(), "Статусы рецептур получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов рецептуры: " + ex.Message);
            }
        }

        // GET: api/statuses/tech-card
        [HttpGet]
        [Route("tech-card")]
        public IHttpActionResult GetTechCardStatuses()
        {
            try
            {
                return OkResponse(GetTechCardStatusList(), "Статусы технологических карт получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов технологической карты: " + ex.Message);
            }
        }

        // GET: api/statuses/lab-batch
        [HttpGet]
        [Route("lab-batch")]
        public IHttpActionResult GetLabBatchStatuses()
        {
            try
            {
                return OkResponse(GetLabBatchStatusList(), "Лабораторные статусы партий получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения лабораторных статусов партий: " + ex.Message);
            }
        }

        // GET: api/statuses/lab-test
        [HttpGet]
        [Route("lab-test")]
        public IHttpActionResult GetLabTestStatuses()
        {
            try
            {
                return OkResponse(GetLabTestStatusList(), "Статусы лабораторных испытаний получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов лабораторных испытаний: " + ex.Message);
            }
        }

        // GET: api/statuses/deviation
        [HttpGet]
        [Route("deviation")]
        public IHttpActionResult GetDeviationSeverities()
        {
            try
            {
                return OkResponse(GetDeviationSeverityList(), "Уровни критичности отклонений получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения уровней отклонений: " + ex.Message);
            }
        }

        // GET: api/statuses/extruder-program
        [HttpGet]
        [Route("extruder-program")]
        public IHttpActionResult GetExtruderProgramStatuses()
        {
            try
            {
                return OkResponse(GetExtruderProgramStatusList(), "Статусы программ экструдера получены");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения статусов программ экструдера: " + ex.Message);
            }
        }

        private object[] GetProductStatusList()
        {
            return new object[]
            {
                new { value = "Активен", label = "Активен" },
                new { value = "Архивирован", label = "Архивирован" }
            };
        }

        private object[] GetOrderStatusList()
        {
            return new object[]
            {
                new { value = "Создан", label = "Создан" },
                new { value = "В работе", label = "В работе" },
                new { value = "Завершён", label = "Завершён" },
                new { value = "Отменён", label = "Отменён" }
            };
        }

        private object[] GetBatchStatusList()
        {
            return new object[]
            {
                new { value = "Подготовлена", label = "Подготовлена" },
                new { value = "В работе", label = "В работе" },
                new { value = "Приостановлена", label = "Приостановлена" },
                new { value = "Завершена", label = "Завершена" },
                new { value = "Заблокирована", label = "Заблокирована" },
                new { value = "Отменена", label = "Отменена" }
            };
        }

        private object[] GetStepStatusList()
        {
            return new object[]
            {
                new { value = "Не начат", label = "Не начат" },
                new { value = "Выполняется", label = "Выполняется" },
                new { value = "Завершён", label = "Завершён" },
                new { value = "Пропущен", label = "Пропущен" }
            };
        }

        private object[] GetRecipeStatusList()
        {
            return new object[]
            {
                new { value = "Черновик", label = "Черновик" },
                new { value = "На согласовании", label = "На согласовании" },
                new { value = "Утверждена", label = "Утверждена" },
                new { value = "Архивирована", label = "Архивирована" }
            };
        }

        private object[] GetTechCardStatusList()
        {
            return new object[]
            {
                new { value = "Черновик", label = "Черновик" },
                new { value = "На согласовании", label = "На согласовании" },
                new { value = "Утверждена", label = "Утверждена" },
                new { value = "Архивирована", label = "Архивирована" }
            };
        }

        private object[] GetLabBatchStatusList()
        {
            return new object[]
            {
                new { value = "Ожидает", label = "Ожидает" },
                new { value = "В работе", label = "В работе" },
                new { value = "Разрешена", label = "Разрешена" },
                new { value = "Заблокирована", label = "Заблокирована" }
            };
        }

        private object[] GetLabTestStatusList()
        {
            return new object[]
            {
                new { value = "Создано", label = "Создано" },
                new { value = "В работе", label = "В работе" },
                new { value = "Завершено", label = "Завершено" },
                new { value = "Отменено", label = "Отменено" }
            };
        }

        private object[] GetDeviationSeverityList()
        {
            return new object[]
            {
                new { value = "Информация", label = "Информация" },
                new { value = "Предупреждение", label = "Предупреждение" },
                new { value = "Критично", label = "Критично" }
            };
        }

        private object[] GetExtruderProgramStatusList()
        {
            return new object[]
            {
                new { value = "Черновик", label = "Черновик" },
                new { value = "Активна", label = "Активна" },
                new { value = "Архивирована", label = "Архивирована" }
            };
        }

        private IHttpActionResult OkResponse(object data, string message)
        {
            return Ok(new
            {
                success = true,
                data,
                message,
                pagination = (object)null
            });
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.InternalServerError, new
            {
                success = false,
                message
            });
        }
    }
}