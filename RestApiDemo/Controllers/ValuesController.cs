using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RestApi;

namespace RestApiDemo.Controllers
{
    [Route("api/[controller]/[action]")]
    [EnableCors("AllowCors")]
    public class ValuesController : ControllerBase
    {
        private readonly IDbConnection _connection;

        public ValuesController(IDbConnection connection)
        {
            _connection = connection;
            GetConnection();
        }

        // GET api/values/get
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST api/values/getData
        [HttpPost]
        public object GetData([FromBody]RequestMetadata request)
        {
            var tableName = request.table.ToLower();
            var sql = "select * from " + tableName;
            return _connection.GetRestData(sql, request);
        }

        // POST api/values/write
        [HttpPost]
        public void Write([FromBody]RequestCrud data)
        {
            _connection.CrudExec(data);
        }

        private IDbConnection GetConnection()
        {
            if (_connection.State == ConnectionState.Open)
            {
                return _connection;
            }
            _connection.Open();
            return _connection;
        }

    }
}
