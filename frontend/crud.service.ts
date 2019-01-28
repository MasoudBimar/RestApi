import {HttpClient, HttpHeaders} from '@angular/common/http';
import {DataSource, RequestMetadata} from '../ng-crud-table';
import {NotifyService} from '../lib/notify/notify.service';
import {AppConfig} from '../../core/app-config';

export class RequestMetadataExt extends RequestMetadata {
  table: string;
}

export class CrudService implements DataSource {

  url: string;
  primaryKeys: string[];
  tableName: string;

  get getUrl(): string { return this._getUrl; }
  set getUrl(val: string) {
    this._getUrl = AppConfig.settings.host + val;
  }
  private _getUrl: string = AppConfig.settings.host + '/flow/getData';

  get writeUrl(): string { return this._writeUrl; }
  set writeUrl(val: string) {
    this._writeUrl = AppConfig.settings.host + val;
  }
  private _writeUrl: string = AppConfig.settings.host + '/flow/write';

  private INSERT = 1;
  private UPDATE = 2;
  private DELETE = 3;

  constructor(private http: HttpClient, private notifyService: NotifyService) {
  }

  get headers() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json');
    return headers;
  }

  getItems(requestMeta: RequestMetadataExt): Promise<any> {
    requestMeta.table = this.tableName;
    return this.http.post(this.getUrl, requestMeta, {headers: this.headers})
      .toPromise()
      .then(res => res)
      .catch(this.handleError.bind(this));
  }

  getItem(row: any): Promise<any> {
    const filters = {};
    for (const key of this.primaryKeys) {
      filters[key] = {value: row[key]};
    }
    const requestMeta = <RequestMetadataExt> {
      pageMeta: {currentPage: 1},
      filters: filters,
      table: this.tableName,
    };
    return this.getItems(requestMeta)
      .then(data => data.items[0]);
  }

  post(row: any): Promise<any> {
    const data = {'table': this.tableName, 'row': row, 'type': this.INSERT};
    return this.http
      .post(this.writeUrl, JSON.stringify(data), {headers: this.headers})
      .toPromise()
      .then(res => res)
      .catch(this.handleError.bind(this));
  }

  put(row: any): Promise<any> {
    const data = {'table': this.tableName, 'row': row, 'type': this.UPDATE};
    return this.http
      .post(this.writeUrl, JSON.stringify(data), {headers: this.headers})
      .toPromise()
      .then(res => res)
      .catch(this.handleError.bind(this));
  }

  delete(row: any): Promise<any> {
    const data = {'table': this.tableName, 'row': row, 'type': this.DELETE};
    return this.http
      .post(this.writeUrl, JSON.stringify(data), {headers: this.headers})
      .toPromise()
      .then(res => res)
      .catch(this.handleError.bind(this));
  }

  private handleError(error: any) {
    const errMsg = error.message ? error.message : error.toString();
    this.notifyService.sendMessage({title: 'HttpErrorResponse', text: errMsg, severity: 'error'});
    console.error(error);
    return Promise.reject(errMsg);
  }

  getOptions(url: string, parentId: any): Promise<any> {
    url = (parentId !== undefined) ? url + '?objId=' + parentId : url;
    return this.http.get(AppConfig.settings.host + url)
      .toPromise()
      .then(response => {
        return response;
      })
      .catch(this.handleError.bind(this));
  }

}
