import { Injectable } from "@angular/core";
import { Subscribable } from "../utils/subscribable";

@Injectable({
  providedIn: 'root'
})
export class ToolbarService {

  public IsSidebarShrank: Subscribable<boolean> = new Subscribable<boolean>();

  public ToggelSidebar() {
    this.IsSidebarShrank.set(!this.IsSidebarShrank.get());
  }

}
