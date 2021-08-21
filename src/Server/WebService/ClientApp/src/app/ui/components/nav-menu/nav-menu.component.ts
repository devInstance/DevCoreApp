import { Component, OnInit } from '@angular/core';
import { ToolbarService } from '../../../services/toolbar.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})

export class NavMenuComponent implements OnInit {
  isExpanded = false;

  constructor(private toolbarSvc: ToolbarService) {
  }

  ngOnInit(): void {
    this.toolbarSvc.IsSidebarShrank.value.subscribe(res => {
      this.isExpanded = res;
    });
  }

  expandSiderbar(): void {
    this.toolbarSvc.ToggelSidebar();
  }
}
