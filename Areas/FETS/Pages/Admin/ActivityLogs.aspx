<%@ Page Title="" Language="C#" MasterPageFile="~/Areas/FETS/Site.Master" AutoEventWireup="true" CodeBehind="ActivityLogs.aspx.cs" Inherits="FETS.Pages.Admin.ActivityLogs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <!-- Font Awesome for better icons -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    
    <div class="container-fluid px-4">
        
        <div class="card mb-4">
            <div class="card-header">
                <i class="fas fa-history me-1"></i>
                User Activity Logs
            </div>
            <div class="card-body">
                <div class="row mb-4">
                    <div class="col-md-3 mb-3">
                        <asp:Label ID="lblUsername" runat="server" Text="Username:" AssociatedControlID="ddlUsername" CssClass="form-label"></asp:Label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="fas fa-user"></i></span>
                            <asp:DropDownList ID="ddlUsername" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilters_Changed">
                                <asp:ListItem Text="All Users" Value=""></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="col-md-3 mb-3">
                        <asp:Label ID="lblAction" runat="server" Text="Action:" AssociatedControlID="ddlAction" CssClass="form-label"></asp:Label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="fas fa-tasks"></i></span>
                            <asp:DropDownList ID="ddlAction" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilters_Changed">
                                <asp:ListItem Text="All Actions" Value=""></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="col-md-3 mb-3">
                        <asp:Label ID="lblEntityType" runat="server" Text="Entity Type:" AssociatedControlID="ddlEntityType" CssClass="form-label"></asp:Label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="fas fa-tag"></i></span>
                            <asp:DropDownList ID="ddlEntityType" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilters_Changed">
                                <asp:ListItem Text="All Types" Value=""></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="col-md-3 mb-3">
                        <asp:Label ID="lblDateRange" runat="server" Text="Date Range:" AssociatedControlID="ddlDateRange" CssClass="form-label"></asp:Label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="fas fa-calendar-alt"></i></span>
                            <asp:DropDownList ID="ddlDateRange" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ApplyFilters_Changed">
                                <asp:ListItem Text="All Time" Value="all" Selected="True"></asp:ListItem>
                                <asp:ListItem Text="Today" Value="today"></asp:ListItem>
                                <asp:ListItem Text="Last 7 Days" Value="week"></asp:ListItem>
                                <asp:ListItem Text="Last 30 Days" Value="month"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                </div>
                
                <div class="table-responsive">
                    <asp:GridView ID="gvActivityLogs" runat="server" CssClass="table table-striped table-hover table-bordered" AutoGenerateColumns="false" 
                        AllowPaging="true" PageSize="20" OnPageIndexChanging="gvActivityLogs_PageIndexChanging" EmptyDataText="No activity logs found." 
                        GridLines="None" CellPadding="4" CellSpacing="0">
                        <PagerSettings Mode="NumericFirstLast" FirstPageText="First" LastPageText="Last" Position="Bottom" />
                        <PagerStyle CssClass="pagination-container" HorizontalAlign="Center" />
                        <Columns>
                            <asp:BoundField DataField="Timestamp" HeaderText="Date & Time" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" SortExpression="Timestamp" />
                            <asp:BoundField DataField="Username" HeaderText="User" SortExpression="Username" />
                            <asp:TemplateField HeaderText="Action" SortExpression="Action">
                                <ItemTemplate>
                                    <span class="badge bg-<%# GetActionBadgeClass(Eval("Action").ToString()) %>"><%# Eval("Action") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                            <asp:BoundField DataField="EntityType" HeaderText="Entity Type" SortExpression="EntityType" />
                            <asp:BoundField DataField="EntityID" HeaderText="Entity ID" SortExpression="EntityID" />
                            <asp:BoundField DataField="IPAddress" HeaderText="IP Address" SortExpression="IPAddress" />
                        </Columns>
                    </asp:GridView>
                </div>
                
                <div class="d-flex justify-content-between align-items-center mt-4">
                    <div>
                        <span class="text-muted">Total Records: <asp:Label ID="lblTotalRecords" runat="server" CssClass="fw-bold">0</asp:Label></span>
                    </div>
                    <asp:Button ID="btnExportCsv" runat="server" Text="Export to CSV" CssClass="btn btn-primary" OnClick="btnExportCsv_Click" />
                </div>
            </div>
        </div>
    </div>
</asp:Content> 