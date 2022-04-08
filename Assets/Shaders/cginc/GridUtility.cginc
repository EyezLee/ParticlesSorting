// utility functions for grid system
uint2 PositionToGrid(float3 pos, float2 spacing)
{
	return uint2(pos.x / spacing.x, pos.y / spacing.y);
}

uint GridToGridID(uint2 grid, uint gridNumX)
{
	return (grid.y * gridNumX + grid.x);
}

uint2 GridIDToGrid(uint id, uint gridNumX)
{
	uint x = id % gridNumX;
	uint y = id / gridNumX;
	return uint2(x, y);
}

bool Intersect(float r, float x, float y, float left, float right, float top, float bottom)
{
	float nearX = max(left, min(x, right));
	float nearY = max(bottom, min(y, top));
	float dx = nearX - x;
	float dy = nearY - y;
	return dx * dx + dy * dy <= r * r;
}