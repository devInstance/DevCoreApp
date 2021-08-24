import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './ui/components/nav-menu/nav-menu.component';
import { HomeComponent } from './ui/pages/home/home.component';
import { CounterComponent } from './ui/pages/counter/counter.component';
import { FetchDataComponent } from './ui/pages/fetch-data/fetch-data.component';
import { ApiAuthorizationModule } from 'src/api-authorization/api-authorization.module';
import { AuthorizeGuard } from 'src/api-authorization/authorize.guard';
import { AuthorizeInterceptor } from 'src/api-authorization/authorize.interceptor';
import { AppToolbarComponent } from './ui/components/toolbar/app-toolbar.component';
import { ToolbarService } from './services/toolbar.service';
import { AuthorizationApi } from './services/net/authorization-api';
import { IAuthorizationApi } from './services/net/api/authorization-api.interface';
import { FetchDataService } from './services/fetchdata.service';
import { IFetchDataApi } from './services/net/api/fetchdata-api.interface';
import { FetchDataApi } from './services/net/fetchdata-api';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    CounterComponent,
    FetchDataComponent,
    AppToolbarComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    //ApiAuthorizationModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'counter', component: CounterComponent },
      { path: 'fetch-data', component: FetchDataComponent/*, canActivate: [AuthorizeGuard]*/ },
    ])
  ],
  providers: [
      //{ provide: HTTP_INTERCEPTORS, useClass: AuthorizeInterceptor, multi: true },
      { provide: IAuthorizationApi, useClass: AuthorizationApi },
      { provide: IFetchDataApi, useClass: FetchDataApi },
      ToolbarService,
      FetchDataService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
