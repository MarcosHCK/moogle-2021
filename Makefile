# Copyright 2021-2025 MarcosHCK
# This file is part of Moogle!.
#
# Moogle! is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Moogle! is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Moogle!. If not, see <http://www.gnu.org/licenses/>.
#

.PHONY: build
build:
	@ dotnet build

.PHONY: dev
dev:
	@ dotnet watch run --project MoogleServer
