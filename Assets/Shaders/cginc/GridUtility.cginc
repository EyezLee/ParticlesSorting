// utility functions for grid system
uint3 PositionToGrid(float3 pos, float3 spacing)
{
	return uint3(pos.x / spacing.x, pos.y / spacing.y, pos.z / spacing.z);
}

uint GridToGridID(uint3 grid, uint gridNumX, uint gridNumY)
{
	return (grid.z * (gridNumY * gridNumX) + grid.y * gridNumX + grid.x);
}

uint3 GridIDToGrid(uint id, uint gridNumX, uint gridNumY)
{
	uint x = id % gridNumX;
	uint y = id / gridNumX;
	uint z = id / (gridNumY * gridNumX);
	return uint3(x, y, z);
}

bool Intersect(float r, float3 pos, float left, float right, float top, float bottom, float front, float back)
{
	float nearX = max(left, min(pos.x, right));
	float nearY = max(bottom, min(pos.y, top));
	float nearZ = max(back, min(pos.z, front));
	float dx = nearX - pos.x;
	float dy = nearY - pos.y;
	float dz = nearZ - pos.z;
	return dx * dx + dy * dy + dz * dz <= r * r;
}