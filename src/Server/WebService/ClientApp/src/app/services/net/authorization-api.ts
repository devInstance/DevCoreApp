import { HttpClient } from "@angular/common/http";
import { Inject } from "@angular/core";
import { Observable } from "rxjs";
import { ChangePasswordParameters } from "../../model/change-password-parameters";
import { ForgotPasswordParameters } from "../../model/forgot-password-parameters";
import { LoginParameters } from "../../model/login-parameters";
import { RegisterParameters } from "../../model/register-parameters";
import { ResetPasswordParameters } from "../../model/reset-password-parameters";
import { UserInfoItem } from "../../model/userinfo-item";
import { IAuthorizationApi } from "./api/authorization-api.interface";

export class AuthorizationApi implements IAuthorizationApi {

    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string)
    {
//        super();
    }

    RegisterAsync(registerParameters: RegisterParameters): Observable<Response> {
        throw new Error("Method not implemented.");
    }
    LoginAsync(loginParameters: LoginParameters): Observable<Response> {
        throw new Error("Method not implemented.");
    }
    LogoutAsync(): Observable<Response> {
        throw new Error("Method not implemented.");
    }
    GetUserInfo(): Observable<UserInfoItem> {
        throw new Error("Method not implemented.");
    }
    DeleteUserAsync(): Observable<boolean> {
        throw new Error("Method not implemented.");
    }
    ChangePasswordAsync(chngParameters: ChangePasswordParameters): Observable<Response> {
        throw new Error("Method not implemented.");
    }
    ForgotPasswordAsync(forgotParameters: ForgotPasswordParameters): Observable<Response> {
        throw new Error("Method not implemented.");
    }
    ResetPasswordAsync(resetPassswordParameters: ResetPasswordParameters): Observable<Response> {
        throw new Error("Method not implemented.");
    }
}
