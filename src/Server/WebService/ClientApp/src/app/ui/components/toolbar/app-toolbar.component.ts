import { Component } from '@angular/core';
import { ToolbarService } from '../../../services/toolbar.service';

@Component({
  selector: 'app-toolbar',
  templateUrl: './app-toolbar.component.html',
  styleUrls: ['./app-toolbar.component.scss']
})
export class AppToolbarComponent {

  constructor(private toolbarSvc: ToolbarService) {
  }

  public ShrinkSidebar() {
    this.toolbarSvc.ToggelSidebar()
  }

  public Logout() {

  }
}
