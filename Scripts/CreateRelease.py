# Installation:
# pip install GitPython
# pip install py7zr
# add msbuild to PATH
# add git to PATH

import os
import git  
import shutil
from py7zr import pack_7zarchive

new_dbm_version = None
new_server_version = None

git_repo = git.Repo(os.getcwd(), search_parent_directories=True)

git_branch = git_repo.active_branch.name
if git_branch != 'main':
    exit('ERROR: You can only create a release from the main branch!')

# Fetch data from git
print('Fetch data from git...')
git_repo.remotes.origin.fetch()

# Ask what projects to release and with what version numbers
print('\nDo you want to release a new version of DigitalBattleMap? (y/n)')
if input() == 'y':
    for i in reversed(range(len(git_repo.tags))):
        tag = str(git_repo.tags[i])
        if 'DigitalBattleMap_' in tag:
            current_version = tag.split('_v')[-1]
            print(f'\nEnter the new version for DigitalBattleMap (last version: {current_version}):')
            new_dbm_version = input()
            break

print('\nDo you want to release a new version of DigitalBattleMapServer? (y/n)')
if input() == 'y':
    for i in reversed(range(len(git_repo.tags))):
        tag = str(git_repo.tags[i])
        if 'DigitalBattleMapServer_' in tag:
            current_version = tag.split('_v')[-1]
            print(f'\nEnter the new version for DigitalBattleMapServer (last version: {current_version}):')
            new_server_version = input()
            break

if new_dbm_version is None and new_server_version is None:
    exit('ERROR: Nothing to release!')

# Print overview of actions
print('\nActions:')
release_name = None
if new_dbm_version is not None:
    # TODO: update version in settings.cs
    print(f'- Create git tag DigitalBattleMap_v{new_dbm_version}')
    print(f'- Create DigitalBattleMap_v{new_dbm_version}.zip')
    release_name = f'DigitalBattleMap_v{new_dbm_version}'
if new_server_version is not None:
    print(f'- Create git tag DigitalBattleMapServer_v{new_server_version}')
    print(f'- Create DigitalBattleMapServer_v{new_server_version}.zip')
    if release_name is None:
        release_name = f'DigitalBattleMapServer_v{new_server_version}'
    else:
        release_name += f' & DigitalBattleMapServer_v{new_server_version}'
    
print('\nAre you sure you want to create a new release? (y/n)')
if input() == 'y':
    # Build solution
    print('Build solution...')
    errorcode = os.system(f'msbuild "{os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMap.sln")}" -t:restore,rebuild -p:RestorePackagesConfig=true,Configuration=Release')
    if errorcode != 0:
        exit("ERROR: Build failed!")

    # Create temp directory
    dbm_release_path = None
    server_release_path = None
    temp_directory = os.path.join(os.getcwd(), "temp")
    if os.path.exists(temp_directory):
        shutil.rmtree(temp_directory)
    shutil.register_archive_format('7zip', pack_7zarchive, description='7zip archive')

    # Create release zip for DigitalBattleMap
    if new_dbm_version is not None:
        # Create release tag
        print(f'Create release tag: DigitalBattleMap_v{new_dbm_version}')
        git_repo.create_tag(f'DigitalBattleMap_v{new_dbm_version}')

        # Create zip
        os.makedirs(temp_directory)
        bin_directory = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMap", "bin", "Release", "net6.0-windows")
        bin_files = [f for f in os.listdir(bin_directory) if os.path.isfile(os.path.join(bin_directory, f))]
        print('Copy binary files...')
        for file in bin_files:
            shutil.copyfile(os.path.join(bin_directory, file), os.path.join(temp_directory, file))
        shutil.copytree(os.path.join(bin_directory, "runtimes", "win"), os.path.join(temp_directory, "runtimes", "win"))
        shutil.copytree(os.path.join(bin_directory, "runtimes", "win-x64"), os.path.join(temp_directory, "runtimes", "win-x64"))
        shutil.copytree(os.path.join(bin_directory, "runtimes", "win-x86"), os.path.join(temp_directory, "runtimes", "win-x86"))
        print('Compress to archive (this can take a while)...')
        dbm_release_path = os.path.join(os.getcwd(), f"DigitalBattleMap_v{new_dbm_version}")
        shutil.make_archive(dbm_release_path, '7zip', temp_directory)
        shutil.rmtree(temp_directory)


    # Create release zip for DigitalBattleMapServer
    if new_server_version is not None:
        # Create release tag
        print(f'Create release tag: DigitalBattleMapServer_v{new_server_version}')
        git_repo.create_tag(f'DigitalBattleMapServer_v{new_server_version}')

        bin_directory = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMapServer", "bin", "Release", "net6.0")
        shutil.copytree(bin_directory, temp_directory)
        print('Compress to archive (this can take a while)...')
        server_release_path = os.path.join(os.getcwd(), f"DigitalBattleMapServer_v{new_server_version}")
        shutil.make_archive(server_release_path, '7zip', temp_directory)
        shutil.rmtree(temp_directory)

    # Print overview with manual actions 
    print('\n=========================================================================')
    print('\nGo to github.com and create a new release with the following parameters:')
    if new_dbm_version is not None:
        print(f'Tag: DigitalBattleMap_v{new_dbm_version}')
    else:
        print(f'Tag: DigitalBattleMapServer_v{new_server_version}')
    print('Target: main')
    print(f'Release title: {release_name}')
    print(f'Attach binaries:') # TODO: add full path to bin
    if dbm_release_path is not None:
        print(f'- {dbm_release_path}.7z')
    if server_release_path is not None:
        print(f'- {server_release_path}.7z')
