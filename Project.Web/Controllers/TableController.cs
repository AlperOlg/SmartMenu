using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Concrete;

namespace Project.Web.Controllers;

public class TableController : Controller
{
    private readonly ITableService _tableService;

    public TableController(ITableService tableService)
    {
        _tableService = tableService;
    }

    [HttpGet]
    public async Task<IActionResult> QrCode(int tableId)
    {
        var table = await _tableService.GetByIdAsync(tableId);
        if (table is null)
        {
            return NotFound();
        }

        var url = Url.Action("Menu", "Customer", new { tableId = table.Id }, Request.Scheme);
        var qrBytes = QrCodeGenerator.GeneratePng(url!);

        return File(qrBytes, "image/png");
    }
}