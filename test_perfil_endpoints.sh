#!/bin/bash
# ================================================================
# Test de los 9 endpoints del Perfil de Usuario — MediTime API
# Compatible con macOS (grep/head)
# ================================================================

API="http://localhost:5020"
PASS=0
FAIL=0
TOTAL=0

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

header() {
    echo ""
    echo -e "${CYAN}═══════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}═══════════════════════════════════════════════════════${NC}"
}

run_test() {
    local nombre="$1"
    local expect_success="$2"
    shift 2
    TOTAL=$((TOTAL + 1))

    # Escribir body a archivo temporal, capturar code por separado
    local tmpfile="/tmp/meditime_test_$$"
    local http_code
    http_code=$(curl -s -o "$tmpfile" -w '%{http_code}' "$@")
    local body
    body=$(cat "$tmpfile" 2>/dev/null)
    rm -f "$tmpfile"

    echo -e "\n${YELLOW}▶ Test #${TOTAL}: ${nombre}${NC}"
    echo -e "  Status HTTP: ${http_code}"

    local passed=false
    if [ "$expect_success" = "yes" ] && [ "$http_code" -ge 200 ] 2>/dev/null && [ "$http_code" -lt 300 ] 2>/dev/null; then
        passed=true
    elif [ "$expect_success" = "no" ] && [ "$http_code" -ge 400 ] 2>/dev/null; then
        passed=true
    fi

    if $passed; then
        PASS=$((PASS + 1))
        if [ "$expect_success" = "no" ]; then
            echo -e "  ${GREEN}✅ PASS (error esperado)${NC}"
        else
            echo -e "  ${GREEN}✅ PASS${NC}"
        fi
    else
        FAIL=$((FAIL + 1))
        echo -e "  ${RED}❌ FAIL${NC}"
    fi

    echo -e "  Body: ${body:0:300}"
    LAST_BODY="$body"
    LAST_CODE="$http_code"
}

# Pre-flight
header "PRE-FLIGHT"
PREFLIGHT=$(curl -s -o /dev/null -w '%{http_code}' "${API}/swagger/index.html" 2>/dev/null)
if [ "$PREFLIGHT" != "200" ]; then
    echo -e "${RED}  ❌ API no responde en ${API}${NC}"
    exit 1
fi
echo -e "${GREEN}  ✅ API accesible${NC}"

# Registro
header "PREPARACIÓN: Registrar usuario de prueba"
TEST_EMAIL="test_perfil_$(date +%s)@meditime.test"
TEST_PASS="TestPass123!"
echo -e "  Email: ${TEST_EMAIL}"

run_test "Registrar usuario" "yes" \
    -X POST "${API}/Usuarios/registro" \
    -H "Content-Type: application/json" \
    -d "{\"nombre\":\"Test\",\"apellidos\":\"Perfil\",\"email\":\"${TEST_EMAIL}\",\"contrasena\":\"${TEST_PASS}\",\"rol\":\"Usuario\",\"esResponsable\":true}"

# Login
header "TEST 1: Login + sesión"
run_test "Login" "yes" \
    -X POST "${API}/Usuarios/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${TEST_EMAIL}\",\"contrasena\":\"${TEST_PASS}\"}"

USER_ID=$(echo "$LAST_BODY" | sed 's/.*"idUsuario":\([0-9]*\).*/\1/')
TOKEN=$(echo "$LAST_BODY" | sed 's/.*"token":"\([^"]*\)".*/\1/')
echo -e "  ${CYAN}→ ID: ${USER_ID}, Token: ${TOKEN:0:15}...${NC}"

if [ -z "$USER_ID" ] || ! [[ "$USER_ID" =~ ^[0-9]+$ ]]; then
    echo -e "${RED}❌ No se pudo obtener el ID. Abortando.${NC}"
    exit 1
fi

# Buscar email
header "TEST 2: Buscar por email"
run_test "Email existente" "yes" "${API}/Usuarios/buscar?email=${TEST_EMAIL}"
run_test "Email inexistente" "yes" "${API}/Usuarios/buscar?email=noexiste_xyz@fake.com"

# Actualizar datos
header "TEST 3: Actualizar datos personales"
run_test "PUT datos" "yes" \
    -X PUT "${API}/Usuarios/${USER_ID}" \
    -H "Content-Type: application/json" \
    -d "{\"nombre\":\"TestActualizado\",\"apellidos\":\"PerfilMod\",\"email\":\"${TEST_EMAIL}\",\"telefono\":\"612345678\",\"fechaNacimiento\":\"1995-06-15\",\"domicilio\":\"Calle Test 123\"}"

run_test "Verificar GET" "yes" "${API}/Usuarios/${USER_ID}"

# Cambiar password
header "TEST 4: Cambiar password"
NEW_PASS="NuevoPass456!"
run_test "Password correcta" "yes" \
    -X POST "${API}/Usuarios/cambiar-password" \
    -H "Content-Type: application/json" \
    -d "{\"id\":${USER_ID},\"passwordActual\":\"${TEST_PASS}\",\"passwordNuevo\":\"${NEW_PASS}\"}"

run_test "Password incorrecta (400)" "no" \
    -X POST "${API}/Usuarios/cambiar-password" \
    -H "Content-Type: application/json" \
    -d "{\"id\":${USER_ID},\"passwordActual\":\"WrongPass\",\"passwordNuevo\":\"Otro\"}"

run_test "Login nueva password" "yes" \
    -X POST "${API}/Usuarios/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${TEST_EMAIL}\",\"contrasena\":\"${NEW_PASS}\"}"

# Notificaciones
header "TEST 5: Notificaciones"
run_test "Guardar notificaciones" "yes" \
    -X PUT "${API}/Usuarios/${USER_ID}/notificaciones" \
    -H "Content-Type: application/json" \
    -d "{\"emailMedicamentos\":false,\"navegadorMedicamentos\":true,\"tiempoAnticipacion\":15,\"nuevasCaracteristicas\":false,\"consejos\":true}"

# Preferencias
header "TEST 6: Preferencias"
run_test "Guardar preferencias" "yes" \
    -X PUT "${API}/Usuarios/${USER_ID}/preferencias" \
    -H "Content-Type: application/json" \
    -d "{\"tema\":\"dark\",\"tamanoTexto\":\"large\",\"vistaCalendario\":\"week\",\"primerDiaSemana\":1,\"idioma\":\"es\",\"formatoHora\":\"24\"}"

run_test "Verificar prefs+notif" "yes" "${API}/Usuarios/${USER_ID}"

# Avatar
header "TEST 7: Avatar Base64"
AVATAR_B64="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPj/HwADBwIAMCbHYQAAAABJRU5ErkJggg=="
run_test "Actualizar avatar" "yes" \
    -X PUT "${API}/Usuarios/${USER_ID}/avatar" \
    -H "Content-Type: application/json" \
    -d "{\"avatarBase64\":\"${AVATAR_B64}\"}"

# Sesiones
header "TEST 8: Sesiones"
run_test "Obtener sesiones" "yes" "${API}/Usuarios/${USER_ID}/sesiones"

header "TEST 9: Cerrar sesiones"
run_test "Cerrar todas" "yes" -X DELETE "${API}/Usuarios/${USER_ID}/sesiones"
run_test "Verificar vacío" "yes" "${API}/Usuarios/${USER_ID}/sesiones"

# Eliminar cuenta
header "TEST 10: Eliminar cuenta"
run_test "DELETE cuenta" "yes" -X DELETE "${API}/Usuarios/${USER_ID}"
run_test "Verificar 404" "no" "${API}/Usuarios/${USER_ID}"
run_test "Verificar CASCADE" "yes" "${API}/Usuarios/buscar?email=${TEST_EMAIL}"

# Resumen
header "RESUMEN"
echo ""
echo -e "  Total:    ${TOTAL}"
echo -e "  ${GREEN}Pasaron:  ${PASS}${NC}"
echo -e "  ${RED}Fallaron: ${FAIL}${NC}"
echo ""
if [ $FAIL -eq 0 ]; then
    echo -e "  ${GREEN}🎉 ¡TODOS LOS TESTS PASARON!${NC}"
else
    echo -e "  ${RED}⚠️  ${FAIL} test(s) fallaron.${NC}"
fi
echo ""
