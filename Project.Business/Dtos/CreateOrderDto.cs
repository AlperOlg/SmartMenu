namespace Project.Business.Dtos;

public class CreateOrderDto
{
    public int TableId { get; set; }
    public int RestaurantId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}