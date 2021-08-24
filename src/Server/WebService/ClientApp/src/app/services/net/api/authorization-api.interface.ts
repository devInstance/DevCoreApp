import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ChangePasswordParameters } from "../../../model/change-password-parameters";
import { ForgotPasswordParameters } from "../../../model/forgot-password-parameters";
import { LoginParameters } from "../../../model/login-parameters";
import { RegisterParameters } from "../../../model/register-parameters";
import { ResetPasswordParameters } from "../../../model/reset-password-parameters";
import { UserInfoItem } from "../../../model/userinfo-item";

@Injectable({
    providedIn: 'root'
})
export abstract class IAuthorizationApi {
    abstract RegisterAsync(registerParameters: RegisterParameters): Observable<Response>;

    abstract LoginAsync(loginParameters: LoginParameters): Observable<Response>;

    abstract LogoutAsync(): Observable<Response>;

    abstract GetUserInfo(): Observable<UserInfoItem>;

    abstract DeleteUserAsync(): Observable<boolean>;

    abstract ChangePasswordAsync(chngParameters: ChangePasswordParameters): Observable<Response>;

    abstract ForgotPasswordAsync(forgotParameters: ForgotPasswordParameters): Observable<Response>;

    abstract ResetPasswordAsync(resetPassswordParameters: ResetPasswordParameters): Observable<Response>;
}
