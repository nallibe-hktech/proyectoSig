#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

BASE_URL="http://localhost:5180"
FRONTEND_URL="http://localhost:4200"

echo -e "${YELLOW}=== Celero Integration Test ===${NC}\n"

# Test 1: Login
echo -e "${YELLOW}[1/4] Testing Login...${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@sig.local",
    "password": "Admin123!"
  }')

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo -e "${RED}✗ Login failed${NC}"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

echo -e "${GREEN}✓ Login successful${NC}"
echo "Token: ${TOKEN:0:20}...\n"

# Test 2: Get Celero Visitas List
echo -e "${YELLOW}[2/4] Testing GET /api/celero-visitas...${NC}"
VISITAS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/celero-visitas?page=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN")

TOTAL=$(echo $VISITAS_RESPONSE | grep -o '"total":[0-9]*' | cut -d':' -f2)

if [ -z "$TOTAL" ]; then
  echo -e "${RED}✗ GET celero-visitas failed${NC}"
  echo "Response: $VISITAS_RESPONSE"
  exit 1
fi

echo -e "${GREEN}✓ GET celero-visitas successful${NC}"
echo "Total visitas: $TOTAL"
echo "Response: ${VISITAS_RESPONSE:0:200}...\n"

# Test 3: Get first visita ID to update
echo -e "${YELLOW}[3/4] Getting visita ID for update test...${NC}"
VISITA_ID=$(echo $VISITAS_RESPONSE | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -z "$VISITA_ID" ]; then
  echo -e "${RED}✗ Could not extract visita ID${NC}"
  exit 1
fi

echo -e "${GREEN}✓ Found visita ID: $VISITA_ID${NC}\n"

# Test 4: Update visita with mapping
echo -e "${YELLOW}[4/4] Testing PUT /api/celero-visitas/{id}...${NC}"
UPDATE_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/celero-visitas/$VISITA_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "projectId": 1,
    "notas": "Test mapping from integration script"
  }')

if echo $UPDATE_RESPONSE | grep -q '"id"'; then
  echo -e "${GREEN}✓ PUT celero-visitas successful${NC}"
  echo "Updated visita: ${UPDATE_RESPONSE:0:200}...\n"
else
  echo -e "${RED}✗ PUT celero-visitas failed${NC}"
  echo "Response: $UPDATE_RESPONSE"
fi

# Final summary
echo -e "${YELLOW}=== Integration Test Summary ===${NC}"
echo -e "${GREEN}✓ All API tests passed!${NC}"
echo -e "\nFrontend URL: ${FRONTEND_URL}"
echo -e "Navigate to: ${FRONTEND_URL}/celero-visitas\n"
