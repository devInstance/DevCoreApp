import { ModelItem } from "./model-item";

// Sync with DevInstance.DevCoreApp.Shared.Model.UserInfoItem
export class UserInfoItem extends ModelItem {
    isAuthenticated: boolean;
    UserName: string;
    exposedClaims: Record<string, string>;
}
