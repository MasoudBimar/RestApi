# .NET Core RESTful API with Dapper

<a target="_blank" href="https://github.com/mazdik/ng-crud-table">Frontend</a>

### Sample Requst POST api/values/getData (RequestMetadata)
```json
{
  "pageMeta": {
    "currentPage": 1,
    "perPage": 50
  },
  "filters": {
    "type": {
      "value": "0",
      "matchMode": "startsWith"
    }
  },
  "sortMeta": [
    {
      "field": "name",
      "order": 1
    }
  ],
  "globalFilterValue": "time",
  "table": "report"
}
```
### Response
```json
{
  "items": [
    {
      "id": 5,
      "name": "Report 1",
      "code": "TIME",
      "type": 0,
      "sort_order": 15,
      "row_cnt": 1,
      "rn": 1
    }
  ],
  "_meta": {
    "totalCount": 1,
    "currentPage": 1,
    "perPage": 50,
    "maxRowCount": 500000
  }
}
```

### Sample POST api/values/write (RequestCrud)
```json
{
  "table": "preferences",
  "row": {
    "preference_name": "test",
    "preference_value": "val"
  },
  "type": 1
}
```

### Data
```cs
    public class RequestMetadata
    {
        public Dictionary<string, FilterMetadata> filters;
        public PageMetadata pageMeta;
        public SortMetadata[] sortMeta;
        public string globalFilterValue;
        public string table;
    }

    public class FilterMetadata
    {
        public dynamic value;
        public string matchMode;
        public dynamic valueTo;
        public string type;
    }

    public class PageMetadata
    {
        public int currentPage;
        public int perPage;
        public int maxRowCount;
    }

    public class SortMetadata
    {
        public string field;
        public int order;
    }
```

### CRUD
```cs
    public class RequestCrud
    {
        public string table;
        public Dictionary<string, object> row;
        public Statement type;
    }

    public enum Statement
    {
        INSERT = 1,
        UPDATE = 2,
        DELETE = 3,
        MERGE = 4
    }
```